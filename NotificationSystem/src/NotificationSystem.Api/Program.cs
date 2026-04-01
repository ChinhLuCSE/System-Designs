using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NotificationSystem.Data.Extensions;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Configuration;
using NotificationSystem.Shared.Extensions;
using NotificationSystem.Shared.Models;
using NotificationSystem.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddNotificationSharedInfrastructure(builder.Configuration);
builder.Services.AddNotificationData(builder.Configuration);

var authOptions = builder.Configuration.GetSection(InfrastructureOptions.SectionName).Get<InfrastructureOptions>()?.Auth ?? new AuthOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOptions.Issuer,
            ValidAudience = authOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

await BootstrapAsync(app);

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/token", () =>
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Audience = authOptions.Audience,
            Issuer = authOptions.Issuer,
            Expires = DateTime.UtcNow.AddHours(8),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, "notification-api-client"),
                new Claim("scope", "notifications.write notifications.read")
            ]),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        });

        return Results.Ok(new { access_token = tokenHandler.WriteToken(token) });
    });
}

var notifications = app.MapGroup("/api/notifications");
notifications.RequireAuthorization();

notifications.MapPost("/", async (
    NotificationRequest request,
    INotificationRepository notificationRepository,
    IIdempotencyStore idempotencyStore,
    IMessagePublisher messagePublisher,
    Microsoft.Extensions.Options.IOptions<InfrastructureOptions> optionsAccessor,
    TimeProvider timeProvider,
    CancellationToken cancellationToken) =>
{
    var options = optionsAccessor.Value;
    var dedupeKey = string.IsNullOrWhiteSpace(request.DedupeKey) ? null : request.DedupeKey.Trim();
    if (dedupeKey is not null)
    {
        var existingNotificationId = await idempotencyStore.GetNotificationIdAsync(dedupeKey, cancellationToken);
        if (existingNotificationId is not null && Guid.TryParse(existingNotificationId, out var existingId))
        {
            var existing = await notificationRepository.GetByIdAsync(existingId, cancellationToken);
            if (existing is not null)
            {
                return Results.Accepted($"/api/notifications/{existing.Id}", new NotificationAcceptedResponse(existing.Id, existing.Status, existing.CorrelationId));
            }
        }
    }

    var now = timeProvider.GetUtcNow();
    var notificationId = Guid.NewGuid();
    var correlationId = Guid.NewGuid().ToString("N");
    var payload = request.Payload ?? new Dictionary<string, string>();
    var metadata = request.Metadata ?? new Dictionary<string, string>();

    if (dedupeKey is not null)
    {
        var reserved = await idempotencyStore.TryReserveAsync(dedupeKey, notificationId.ToString(), TimeSpan.FromHours(24), cancellationToken);
        if (!reserved)
        {
            var existingNotificationId = await idempotencyStore.GetNotificationIdAsync(dedupeKey, cancellationToken);
            if (existingNotificationId is not null && Guid.TryParse(existingNotificationId, out var existingId))
            {
                var existing = await notificationRepository.GetByIdAsync(existingId, cancellationToken);
                if (existing is not null)
                {
                    return Results.Accepted($"/api/notifications/{existing.Id}", new NotificationAcceptedResponse(existing.Id, existing.Status, existing.CorrelationId));
                }
            }
        }
    }

    var notificationRecord = new NotificationRecord(
        notificationId,
        request.UserId,
        request.Channel,
        request.Priority,
        request.BlueprintId,
        JsonMessageSerializer.Serialize(payload),
        request.SourceService,
        dedupeKey,
        correlationId,
        NotificationStatus.Pending,
        0,
        LastError: null,
        now,
        now,
        SentAt: null);

    await notificationRepository.CreateAsync(notificationRecord, cancellationToken);

    var envelope = new NotificationEnvelope(
        notificationId,
        request.UserId,
        request.Channel,
        request.Priority,
        request.BlueprintId,
        payload,
        0,
        correlationId,
        now,
        request.SourceService,
        metadata);

    var acceptedAudit = new AuditEvent(notificationId, correlationId, request.SourceService, request.Channel, request.Priority, NotificationStatus.Pending, AuditEventType.Accepted, 0, "Notification accepted by API.", now);
    await messagePublisher.PublishAsync(options.Kafka.Topics.Audit, notificationId.ToString("N"), acceptedAudit, cancellationToken);
    await messagePublisher.PublishAsync(TopicNameResolver.ResolvePriorityTopic(request.Priority, options.Kafka.Topics), notificationId.ToString("N"), envelope, cancellationToken);
    await notificationRepository.UpdateStatusAsync(notificationId, NotificationStatus.Queued, 0, lastError: null, sentAt: null, cancellationToken);
    await messagePublisher.PublishAsync(options.Kafka.Topics.Audit, notificationId.ToString("N"), acceptedAudit with { Status = NotificationStatus.Queued, EventType = AuditEventType.Queued, Details = "Notification queued to priority topic." }, cancellationToken);

    return Results.Accepted($"/api/notifications/{notificationId}", new NotificationAcceptedResponse(notificationId, NotificationStatus.Queued, correlationId));
})
.WithName("CreateNotification");

notifications.MapGet("/{id:guid}", async (Guid id, INotificationRepository notificationRepository, IAuditRepository auditRepository, CancellationToken cancellationToken) =>
{
    var notification = await notificationRepository.GetByIdAsync(id, cancellationToken);
    if (notification is null)
    {
        return Results.NotFound();
    }

    var latestAudit = await auditRepository.GetLatestAsync(id, cancellationToken);
    return Results.Ok(new NotificationDetailsResponse(
        notification.Id,
        notification.UserId,
        notification.Channel,
        notification.Priority,
        notification.Status,
        notification.AttemptCount,
        notification.LastError,
        notification.CreatedAt,
        notification.SentAt,
        latestAudit));
})
.WithName("GetNotification");

app.Run();

static async Task BootstrapAsync(WebApplication app)
{
    var notificationRepository = app.Services.GetRequiredService<INotificationRepository>();
    var auditRepository = app.Services.GetRequiredService<IAuditRepository>();
    var topicBootstrapper = app.Services.GetRequiredService<KafkaTopicBootstrapper>();

    await topicBootstrapper.EnsureTopicsAsync(CancellationToken.None);
    await notificationRepository.EnsureSchemaAsync(CancellationToken.None);
    await notificationRepository.SeedAsync(CancellationToken.None);
    await auditRepository.EnsureSchemaAsync(CancellationToken.None);
}

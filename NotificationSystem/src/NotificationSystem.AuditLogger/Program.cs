using NotificationSystem.Data.Extensions;
using NotificationSystem.Shared.Abstractions;
using NotificationSystem.Shared.Extensions;
using NotificationSystem.Shared.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddNotificationSharedInfrastructure(builder.Configuration);
builder.Services.AddNotificationData(builder.Configuration);
builder.Services.AddHostedService<AuditLoggingBackgroundService>();

var host = builder.Build();
await host.Services.GetRequiredService<IAuditRepository>().EnsureSchemaAsync(CancellationToken.None);
await host.RunAsync();

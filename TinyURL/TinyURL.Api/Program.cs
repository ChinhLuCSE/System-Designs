using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using TinyURL.Api.Abstractions;
using TinyURL.Api.Contracts;
using TinyURL.Api.Infrastructure;
using TinyURL.Api.Options;
using TinyURL.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<TinyUrlOptions>()
    .Bind(builder.Configuration.GetSection(TinyUrlOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<CassandraOptions>()
    .Bind(builder.Configuration.GetSection(CassandraOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<RedisOptions>()
    .Bind(builder.Configuration.GetSection(RedisOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<ZooKeeperOptions>()
    .Bind(builder.Configuration.GetSection(ZooKeeperOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("create-url", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

builder.Services.AddSingleton<IUrlRepository, CassandraUrlRepository>();
builder.Services.AddSingleton<IUrlCache, RedisUrlCache>();
builder.Services.AddSingleton<IRangeAllocator, ZooKeeperRangeAllocator>();
builder.Services.AddSingleton<Base62Encoder>();
builder.Services.AddSingleton<IShortUrlService, ShortUrlService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await app.Services.InitializeInfrastructureAsync();

app.MapPost("/create-url", async Task<Results<Created<CreateShortUrlResponse>, ValidationProblem>> (
        CreateShortUrlRequest request,
        IShortUrlService shortUrlService,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        if (!Uri.TryCreate(request.LongUrl, UriKind.Absolute, out var longUrl) ||
            (longUrl.Scheme != Uri.UriSchemeHttp && longUrl.Scheme != Uri.UriSchemeHttps))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["longUrl"] = ["Only absolute HTTP/HTTPS URLs are supported."]
            });
        }

        var result = await shortUrlService.CreateAsync(longUrl.ToString(), cancellationToken);
        var resourceUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{result.ShortCode}";

        return TypedResults.Created(resourceUri, result);
    })
    .RequireRateLimiting("create-url")
    .WithName("CreateUrl");

app.MapGet("/{shortCode}", async Task<Results<RedirectHttpResult, NotFound>> (
        string shortCode,
        IShortUrlService shortUrlService,
        CancellationToken cancellationToken) =>
    {
        var longUrl = await shortUrlService.ResolveAsync(shortCode, cancellationToken);
        return longUrl is null
            ? TypedResults.NotFound()
            : TypedResults.Redirect(longUrl, permanent: true);
    })
    .WithName("ResolveShortUrl");

app.MapHealthChecks("/health");

app.Run();

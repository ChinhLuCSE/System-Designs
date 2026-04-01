using NotificationSystem.Data.Extensions;
using NotificationSystem.Shared.Extensions;
using NotificationSystem.Shared.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddNotificationSharedInfrastructure(builder.Configuration);
builder.Services.AddNotificationData(builder.Configuration);
builder.Services.AddHostedService<ChannelDeliveryBackgroundService>();

var host = builder.Build();
await host.RunAsync();

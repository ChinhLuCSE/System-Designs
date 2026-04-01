# NotificationSystem.Worker.Email

## Purpose

Email delivery worker.

This service consumes email-channel notifications and attempts delivery through the fake email provider implementation.

## Responsibilities

- Consume `channel.email`
- Resolve destination/device settings
- Call fake email provider
- Update notification status in MySQL
- Retry transient failures with exponential backoff
- Move unrecoverable failures to `dlq.email`
- Publish delivery audit events

## Main Files

- [Program.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Worker.Email/Program.cs)
- [appsettings.json](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Worker.Email/appsettings.json)
- [ChannelDeliveryBackgroundService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/ChannelDeliveryBackgroundService.cs)
- [ChannelDeliveryService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/ChannelDeliveryService.cs)
- [FakeNotificationProviders.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/FakeNotificationProviders.cs)

## Runtime Notes

- Uses fake providers only in this phase
- Success path updates status to `Sent`
- Transient failures are retried before DLQ

## Related Components

- Receives work from `NotificationSystem.Processor`
- Shares delivery logic with SMS and Push workers

## Read Next

- [System-Components-Overview.md](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/docs/System-Components-Overview.md)

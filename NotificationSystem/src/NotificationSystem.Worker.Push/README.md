# NotificationSystem.Worker.Push

## Purpose

Push notification delivery worker.

This service consumes push notifications and sends them through the fake push provider implementation.

## Responsibilities

- Consume `channel.push`
- Resolve active push destination/device token
- Call fake push provider
- Update notification state in MySQL
- Retry transient failures
- Publish to `dlq.push` on unrecoverable failure
- Publish audit events

## Main Files

- [Program.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Worker.Push/Program.cs)
- [appsettings.json](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Worker.Push/appsettings.json)
- [ChannelDeliveryBackgroundService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/ChannelDeliveryBackgroundService.cs)
- [ChannelDeliveryService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/ChannelDeliveryService.cs)

## Runtime Notes

- Push logic is still mock-based in v1
- Intended future real adapters include `FCM` and `APNs`

## Related Components

- Receives channel work from `NotificationSystem.Processor`
- Shares delivery flow with email and SMS workers

## Read Next

- [System-Components-Overview.md](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/docs/System-Components-Overview.md)

# NotificationSystem.Worker.Sms

## Purpose

SMS delivery worker.

This service consumes SMS notifications and sends them through the fake SMS provider implementation.

## Responsibilities

- Consume `channel.sms`
- Resolve recipient destination
- Call fake SMS provider
- Update MySQL status
- Retry transient failures
- Publish to `dlq.sms` for unrecoverable failures
- Publish audit events

## Main Files

- [Program.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Worker.Sms/Program.cs)
- [appsettings.json](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Worker.Sms/appsettings.json)
- [ChannelDeliveryBackgroundService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/ChannelDeliveryBackgroundService.cs)
- [ChannelDeliveryService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/ChannelDeliveryService.cs)

## Runtime Notes

- Uses the same shared worker flow as email and push
- Intended to be swapped with a real provider adapter in a later phase

## Related Components

- Receives work from `NotificationSystem.Processor`
- Emits audit events consumed by `NotificationSystem.AuditLogger`

## Read Next

- [System-Components-Overview.md](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/docs/System-Components-Overview.md)

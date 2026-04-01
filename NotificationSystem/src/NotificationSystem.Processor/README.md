# NotificationSystem.Processor

## Purpose

Priority queue processor for accepted notifications.

This service consumes Kafka priority topics, validates whether a notification is still eligible for delivery, and routes it to the correct channel topic.

## Responsibilities

- Consume `notifications.high`, `notifications.medium`, `notifications.low`
- Prefer higher-priority topics before lower-priority ones
- Check active consent/device settings
- Enforce rate limits via Redis
- Load blueprints from Redis or MySQL
- Update operational status in MySQL
- Publish processing audit events
- Forward messages to `channel.email`, `channel.sms`, or `channel.push`

## Main Files

- [Program.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Processor/Program.cs)
- [appsettings.json](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Processor/appsettings.json)
- [PriorityProcessingBackgroundService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/PriorityProcessingBackgroundService.cs)
- [NotificationProcessingService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/NotificationProcessingService.cs)

## Runtime Notes

- Runs as a background worker
- Reads from Kafka and writes to Kafka/MySQL/Redis
- This service contains the main business-processing decision points

## Related Components

- Consumes work created by `NotificationSystem.Api`
- Produces work for channel workers
- Emits audit events for `NotificationSystem.AuditLogger`

## Read Next

- [System-Components-Overview.md](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/docs/System-Components-Overview.md)

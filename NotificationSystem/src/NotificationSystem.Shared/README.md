# NotificationSystem.Shared

## Purpose

Shared contracts and reusable services across the notification system.

This project contains the common types and orchestration services that allow all runtime services to speak the same message format and follow the same processing rules.

## Responsibilities

- Define notification contracts and enums
- Define interfaces for repositories, publishers, providers, caches, and limiters
- Provide Kafka message publishing
- Provide Redis idempotency, blueprint cache, and rate limiter implementations
- Provide fake delivery providers
- Provide processing and delivery orchestration services
- Provide hosted background services used by executables

## Main Areas

- `Abstractions/`
- `Models/`
- `Configuration/`
- `Extensions/`
- `Services/`

## Key Files

- [Contracts.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Models/Contracts.cs)
- [Enums.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Models/Enums.cs)
- [InfrastructureServiceCollectionExtensions.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Extensions/InfrastructureServiceCollectionExtensions.cs)
- [NotificationProcessingService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/NotificationProcessingService.cs)
- [ChannelDeliveryService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/ChannelDeliveryService.cs)

## Runtime Notes

- This project does not run by itself
- It is the glue layer used by API, processor, workers, and audit logger

## Read Next

- [System-Components-Overview.md](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/docs/System-Components-Overview.md)

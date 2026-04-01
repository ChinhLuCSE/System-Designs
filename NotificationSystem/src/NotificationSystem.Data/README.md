# NotificationSystem.Data

## Purpose

Persistence layer for the notification system.

This project contains the concrete repositories and connection factories for MySQL and Cassandra.

## Responsibilities

- Open and retry MySQL connections
- Create and seed MySQL schema
- Read and update notification operational state
- Read active device settings and blueprints
- Connect to Cassandra with retry
- Create Cassandra keyspace/table for audit events
- Persist and query audit events

## Main Files

- [DataServiceCollectionExtensions.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Data/Extensions/DataServiceCollectionExtensions.cs)
- [MySqlConnectionFactory.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Data/Persistence/MySqlConnectionFactory.cs)
- [MySqlNotificationRepository.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Data/Persistence/MySqlNotificationRepository.cs)
- [CassandraAuditRepository.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Data/Persistence/CassandraAuditRepository.cs)

## Runtime Notes

- MySQL is used for current operational state
- Cassandra is used for append-only audit history
- Connection/bootstrap retry logic is included to make docker-compose startup more resilient

## Related Components

- Used by API, processor, workers, and audit logger
- Works together with models/interfaces defined in `NotificationSystem.Shared`

## Read Next

- [System-Components-Overview.md](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/docs/System-Components-Overview.md)

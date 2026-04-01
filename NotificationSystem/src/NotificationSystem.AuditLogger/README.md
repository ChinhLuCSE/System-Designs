# NotificationSystem.AuditLogger

## Purpose

Audit event writer for the notification system.

This service consumes audit events from Kafka and stores them in Cassandra as append-only history.

## Responsibilities

- Consume `notifications.audit`
- Ensure Cassandra schema exists
- Persist audit events in Cassandra
- Support latest-audit lookup through the shared repository

## Main Files

- [Program.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.AuditLogger/Program.cs)
- [appsettings.json](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.AuditLogger/appsettings.json)
- [AuditLoggingBackgroundService.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Shared/Services/AuditLoggingBackgroundService.cs)
- [CassandraAuditRepository.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Data/Persistence/CassandraAuditRepository.cs)

## Runtime Notes

- Stores immutable audit trail separate from operational MySQL state
- This service is write-heavy by design

## Related Components

- Consumes audit events emitted by API, processor, and workers
- Feeds the latest audit state shown by the API

## Read Next

- [System-Components-Overview.md](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/docs/System-Components-Overview.md)

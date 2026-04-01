# NotificationSystem.Api

## Purpose

Public ingress API for the notification system.

This service receives notification requests from upstream services, validates JWT bearer tokens, applies idempotency checks, stores the initial notification record, and publishes the request to Kafka priority topics.

## Responsibilities

- Expose `POST /api/notifications`
- Expose `GET /api/notifications/{id}`
- Validate `JWT` tokens
- Reserve dedupe keys in Redis
- Persist notification state in MySQL
- Publish accepted and queued audit events
- Publish to priority topics in Kafka
- Bootstrap MySQL schema, seed data, Cassandra schema, and Kafka topics on startup

## Main Files

- [Program.cs](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Api/Program.cs)
- [appsettings.json](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/src/NotificationSystem.Api/appsettings.json)

## Runtime Notes

- Local dev includes `POST /dev/token` for generating a test token
- API is exposed through Nginx at `http://localhost:8080`
- This service is the first entrypoint in the end-to-end flow

## Related Components

- Writes operational data through `NotificationSystem.Data`
- Uses shared contracts/services from `NotificationSystem.Shared`
- Publishes work for `NotificationSystem.Processor`

## Read Next

- [System-Components-Overview.md](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/docs/System-Components-Overview.md)
- [README.md](/Users/ADMIN/Desktop/Projects/System-Designs/NotificationSystem/README.md)

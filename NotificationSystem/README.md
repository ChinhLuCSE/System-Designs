# Notification System

Advanced notification system implementation based on [Notification-System-Design.md](./Notification-System-Design.md), using `.NET 10`, `MySQL`, `Redis`, `Kafka`, `Cassandra`, and `Nginx`. Monitoring/analytics are intentionally excluded for this iteration.

## Services

- `notification-api`: authenticated ingress, idempotency, persistence, Kafka priority publish
- `notification-processor`: consent + rate-limit checks, blueprint cache resolution, channel routing
- `worker-email`, `worker-sms`, `worker-push`: fake provider delivery, retry, DLQ
- `audit-logger`: Cassandra audit trail writer
- infra: `mysql`, `redis`, `kafka`, `cassandra`, `nginx`

## Run locally

```bash
docker compose up --build
```

API is exposed through Nginx at [http://localhost:8080](http://localhost:8080).

## Get a dev token

```bash
curl -X POST http://localhost:8080/dev/token
```

Copy `access_token` from the response.

## Create a notification

```bash
curl -X POST http://localhost:8080/api/notifications \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user-1",
    "channel": "Email",
    "priority": "High",
    "blueprintId": "welcome-email",
    "payload": {
      "message": "hello from compose"
    },
    "dedupeKey": "demo-order-1001",
    "metadata": {
      "category": "billing"
    },
    "sourceService": "orders-service"
  }'
```

## Query a notification

```bash
curl http://localhost:8080/api/notifications/<NOTIFICATION_ID> \
  -H "Authorization: Bearer <TOKEN>"
```

## Seeded data

- users: `user-1`, `user-2`, `user-3`
- blueprints: `welcome-email`, `otp-sms`, `billing-push`
- failure simulation:
  - `user-2` email destination triggers transient failures before succeeding
  - `user-3` email destination triggers permanent failures and DLQ flow

## Third-party prep

Provider integrations are intentionally mocked in v1. Preparation work for real providers is documented in [docs/ThirdParty-Preparation.md](./docs/ThirdParty-Preparation.md).

## Documentation

For architecture and onboarding docs, start here:

- [docs/](./docs)
- [System-Components-Overview.md](./docs/System-Components-Overview.md)
- [Onboarding-Path.md](./docs/Onboarding-Path.md)
- [Sequence-Diagrams.md](./docs/Sequence-Diagrams.md)
- [ThirdParty-Preparation.md](./docs/ThirdParty-Preparation.md)

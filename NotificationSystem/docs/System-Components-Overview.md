# Notification System Components Overview

Tài liệu này giải thích từng thành phần của notification system đã được triển khai trong repo, theo kiểu “README của từng project” nhưng gom lại thành một bản tổng quan để dễ onboarding và review kiến trúc.

## 1. Mục tiêu hệ thống

Hệ thống này nhận yêu cầu gửi notification từ các upstream services, đưa message vào hàng đợi ưu tiên, xử lý consent/rate limit, chuyển sang worker theo channel, gửi qua provider giả lập, rồi ghi audit trail.

Phạm vi hiện tại:

- Có `JWT auth` cho service gọi vào API
- Có `priority queue` bằng Kafka topic tách theo `High`, `Medium`, `Low`
- Có worker tách theo channel `Email`, `Sms`, `Push`
- Có `MySQL` cho operational data
- Có `Redis` cho idempotency, rate limit, blueprint cache
- Có `Cassandra` cho audit log
- Có `Nginx` làm entrypoint/load balancer local
- Chưa có monitoring/analytics service
- Chưa tích hợp provider thật, đang dùng fake providers để local chạy end-to-end

## 2. Bố cục source code

### `src/NotificationSystem.Api`

Service API nhận request từ upstream systems.

Nhiệm vụ chính:

- validate `JWT bearer token`
- nhận `POST /api/notifications`
- chống duplicate bằng dedupe key trong Redis
- lưu notification record ban đầu vào MySQL
- publish message sang Kafka priority topic
- expose `GET /api/notifications/{id}` để đọc trạng thái hiện tại
- expose `POST /dev/token` cho local development

Đây là “cổng vào” của toàn hệ thống.

### `src/NotificationSystem.Processor`

Background worker xử lý message từ priority topics.

Nhiệm vụ chính:

- consume lần lượt từ `notifications.high`, rồi `medium`, rồi `low`
- kiểm tra user có consent cho channel hay không
- kiểm tra rate limit
- nạp blueprint từ Redis hoặc MySQL
- cập nhật trạng thái notification trong MySQL
- publish sang channel topic tương ứng
- publish audit events

Đây là lớp “business processing” trung tâm.

### `src/NotificationSystem.Worker.Email`

Worker chuyên xử lý message của channel email.

Nhiệm vụ chính:

- consume từ topic `channel.email`
- gọi fake email provider
- nếu thành công thì đánh dấu `Sent`
- nếu lỗi transient thì retry với exponential backoff
- nếu lỗi permanent hoặc quá số lần retry thì đẩy sang DLQ
- ghi audit event

### `src/NotificationSystem.Worker.Sms`

Giống email worker nhưng dành cho `SMS`.

Consume từ `channel.sms`, gọi fake SMS provider, retry/DLQ/audit tương tự.

### `src/NotificationSystem.Worker.Push`

Giống email worker nhưng dành cho `Push`.

Consume từ `channel.push`, gọi fake push provider, retry/DLQ/audit tương tự.

### `src/NotificationSystem.AuditLogger`

Background worker ghi audit trail vào Cassandra.

Nhiệm vụ chính:

- consume từ topic `notifications.audit`
- ghi immutable audit events vào Cassandra
- hỗ trợ API đọc audit event mới nhất của một notification

Đây là lớp “write-heavy audit storage”.

### `src/NotificationSystem.Shared`

Thư viện dùng chung cho toàn bộ services.

Nội dung chính:

- domain models và contracts
- enums như `NotificationChannel`, `NotificationPriority`, `NotificationStatus`
- interfaces như repository, publisher, provider, cache, rate limiter
- Kafka publisher
- Redis idempotency store
- Redis rate limiter
- Redis blueprint cache
- fake providers
- processor service
- channel delivery service
- background services cho processor, workers, audit logger

Project này giúp các service dùng chung một wire contract và cùng một business flow.

### `src/NotificationSystem.Data`

Lớp persistence.

Nội dung chính:

- `MySqlConnectionFactory`
- `MySqlNotificationRepository`
- `CassandraAuditRepository`
- DI registration cho data layer

Project này tách logic đọc/ghi dữ liệu khỏi service runtime.

### `tests/NotificationSystem.Tests`

Unit tests cho decision logic quan trọng.

Hiện đang cover:

- processor cancel khi không có active device setting
- processor publish sang channel topic khi request hợp lệ
- worker retry khi gặp transient failure
- worker đẩy DLQ khi gặp permanent failure

## 3. Flow end-to-end

### Bước 1: Upstream gọi API

Service upstream gọi `POST /api/notifications` qua Nginx.

API:

- xác thực JWT
- kiểm tra dedupe key trong Redis
- tạo `notification_id` và `correlation_id`
- lưu notification ở trạng thái ban đầu trong MySQL
- publish audit event `Accepted`
- publish message sang priority topic trong Kafka
- cập nhật status thành `Queued`

### Bước 2: Processor lấy message từ priority queue

Processor đọc từ Kafka theo bias ưu tiên:

1. `notifications.high`
2. `notifications.medium`
3. `notifications.low`

Sau đó:

- kiểm tra device setting active trong MySQL
- nếu không có consent thì `Cancelled`
- kiểm tra rate limit trong Redis
- nếu vượt quota thì `Cancelled`
- load blueprint từ Redis, fallback MySQL
- cập nhật status `Processing`
- publish message sang channel topic
- publish audit event tương ứng

### Bước 3: Channel worker gửi notification

Worker theo channel consume message:

- `worker-email` đọc `channel.email`
- `worker-sms` đọc `channel.sms`
- `worker-push` đọc `channel.push`

Worker:

- lấy destination/device setting
- gọi fake provider
- nếu success: update `Sent`
- nếu transient fail: requeue với `attempt_count + 1`
- nếu exhausted/permanent fail: update `DeadLettered` và publish sang DLQ topic
- luôn publish audit event

### Bước 4: Audit logger ghi lịch sử

`audit-logger` đọc mọi event từ `notifications.audit` và append vào Cassandra.

API khi query notification sẽ:

- đọc trạng thái operational từ MySQL
- đọc latest audit event từ Cassandra

## 4. Hạ tầng và vai trò của từng thành phần

### Nginx

Vai trò:

- public entrypoint local tại `http://localhost:8080`
- reverse proxy tới `notification-api`
- giữ chỗ cho load balancing khi scale API

Lý do có mặt:

- bám sát design có load balancer
- mô phỏng entrypoint production rõ ràng hơn là expose API trực tiếp

### Kafka

Vai trò:

- decouple request ingest khỏi processing
- tách priority queue
- tách channel queue
- chứa audit stream
- chứa DLQ cho failure cases

Topic hiện tại:

- `notifications.high`
- `notifications.medium`
- `notifications.low`
- `channel.email`
- `channel.sms`
- `channel.push`
- `notifications.audit`
- `dlq.email`
- `dlq.sms`
- `dlq.push`

### MySQL

Vai trò:

- lưu dữ liệu quan hệ và trạng thái vận hành

Tables hiện tại:

- `users`
- `device_settings`
- `notification_blueprints`
- `notifications`

MySQL là source of truth cho status hiện tại của từng notification.

### Redis

Vai trò:

- idempotency store theo dedupe key
- rate limit counters
- cache blueprint

Redis được dùng cho fast lookup và short-lived state.

### Cassandra

Vai trò:

- append-only audit/event store

Vì audit là workload write-heavy, Cassandra phù hợp hơn relational DB cho phần này.

## 5. Contract chính của hệ thống

### API request

`POST /api/notifications`

Payload chính:

- `userId`
- `channel`
- `priority`
- `blueprintId`
- `payload`
- `dedupeKey`
- `metadata`
- `sourceService`

### API response

Trả `202 Accepted` với:

- `notificationId`
- `status`
- `correlationId`

### Internal envelope

Message nội bộ giữa API, processor và workers dùng `NotificationEnvelope`.

Trường quan trọng:

- `notificationId`
- `userId`
- `channel`
- `priority`
- `blueprintId`
- `payload`
- `attemptCount`
- `correlationId`
- `requestedAt`
- `sourceService`
- `metadata`

### Audit event

Mọi state transition quan trọng đều được publish dưới dạng `AuditEvent`.

## 6. Trạng thái notification

Các trạng thái đang dùng:

- `Pending`: vừa nhận request
- `Queued`: đã đưa vào Kafka
- `Processing`: processor đã duyệt và chuyển sang channel phase
- `Sent`: worker gửi thành công
- `Failed`: lỗi tạm thời trong quá trình gửi
- `Cancelled`: bị hủy do consent/rate limit/no destination
- `DeadLettered`: lỗi không recover được hoặc vượt retry limit

## 7. Retry và failure handling

### Transient failure

Fake provider có thể mô phỏng transient failure.

Khi gặp transient failure:

- worker tăng `attemptCount`
- chờ theo exponential backoff
- publish lại vào channel topic
- ghi audit event `RetryScheduled`

### Permanent failure

Khi gặp permanent failure:

- worker update status `DeadLettered`
- publish message sang DLQ topic
- ghi audit event `DeadLettered`

### Cancelled cases

Notification có thể bị `Cancelled` nếu:

- user không có active device setting cho channel
- rate limit bị vượt
- worker không tìm thấy destination hợp lệ

## 8. Seed data và mô phỏng local

Repo có seed data để test local nhanh:

- `user-1`: đường đi thành công
- `user-2`: email có transient failure trước khi thành công
- `user-3`: email có permanent failure để test DLQ

Blueprints mẫu:

- `welcome-email`
- `otp-sms`
- `billing-push`

## 9. Docker Compose view

`docker-compose.yml` hiện dựng các container sau:

- `nginx`
- `notification-api`
- `notification-processor`
- `worker-email`
- `worker-sms`
- `worker-push`
- `audit-logger`
- `mysql`
- `redis`
- `kafka`
- `cassandra`

Điểm đáng chú ý:

- app services có retry logic để vượt qua startup race khi infra còn đang warm up
- Nginx dùng Docker DNS resolver động để không bị giữ upstream IP cũ sau khi container API bị recreate
- Kafka topics được bootstrap từ app side
- MySQL schema và seed data được bootstrap từ API startup
- Cassandra keyspace/table được bootstrap từ audit repository

## 10. Những gì đang là mock hoặc chưa production-ready

### Fake providers

Email/SMS/Push hiện chỉ là provider giả lập.

Mục đích:

- cho phép local flow chạy xuyên suốt
- giữ abstraction sạch để phase sau cắm provider thật

### Dev token endpoint

`POST /dev/token` chỉ phục vụ local development.

Production thực tế sẽ cần thay bằng issuer/service auth chuẩn hơn.

### Warnings còn tồn tại

Build hiện vẫn có warning từ dependency `Newtonsoft.Json` transitively đi theo Cassandra driver. Hệ thống chạy được, nhưng đây là điểm cần dọn cho phase hardening.

## 11. Nên đọc file nào khi muốn hiểu sâu hơn

Nếu muốn hiểu nhanh nhất:

1. [README.md](../README.md)
2. [Notification-System-Design.md](../Notification-System-Design.md)
3. [Program.cs](../src/NotificationSystem.Api/Program.cs)
4. [NotificationProcessingService.cs](../src/NotificationSystem.Shared/Services/NotificationProcessingService.cs)
5. [ChannelDeliveryService.cs](../src/NotificationSystem.Shared/Services/ChannelDeliveryService.cs)
6. [MySqlNotificationRepository.cs](../src/NotificationSystem.Data/Persistence/MySqlNotificationRepository.cs)
7. [docker-compose.yml](../docker-compose.yml)

## 12. Tóm tắt ngắn

Nếu nhìn ở mức cao nhất, hệ thống đang chạy theo chuỗi này:

`Nginx -> Notification API -> Kafka priority topics -> Processor -> Kafka channel topics -> Channel workers -> Cassandra audit + MySQL status`

Đây là một bản v1 đã có thể chạy local end-to-end, đủ tốt để:

- demo flow notification hoàn chỉnh
- tiếp tục mở rộng sang provider thật
- thêm analytics/monitoring ở phase sau
- scale từng phần độc lập theo đúng tinh thần microservice/event-driven design

# Onboarding Path

Tài liệu này dành cho người mới vào repo và muốn hiểu hệ thống nhanh, theo thứ tự đọc hợp lý thay vì mở file ngẫu nhiên.

## Mục tiêu

Sau khoảng 30-45 phút, người đọc nên hiểu được:

- hệ thống này giải quyết bài toán gì
- từng service chịu trách nhiệm gì
- request đi qua những đâu từ lúc vào API đến lúc `Sent` hoặc `Cancelled`
- dữ liệu nào nằm ở `MySQL`, `Redis`, `Kafka`, `Cassandra`
- nên sửa chỗ nào nếu muốn thêm behavior mới

## Lộ trình 10 phút đầu

### 1. Đọc mô tả cấp cao

Đọc:

- [README.md](./README.md)
- [Notification-System-Design.md](./Notification-System-Design.md)

Mục tiêu:

- hiểu bài toán notification system
- hiểu repo đang bám theo bản design advanced
- biết các thành phần chính trong local stack

### 2. Đọc doc tổng quan hệ thống

Đọc:

- [System-Components-Overview.md](./docs/System-Components-Overview.md)

Mục tiêu:

- nắm được full flow end-to-end
- biết vai trò từng project trong `src/`

## Lộ trình 20 phút tiếp theo

### 3. Đọc service entrypoint trước

Đọc theo thứ tự:

1. [Program.cs](../src/NotificationSystem.Api)
2. [Program.cs](../src/NotificationSystem.Processor)
3. [Program.cs](../src/NotificationSystem.Worker.Email)
4. [Program.cs](../src/NotificationSystem.AuditLogger)

Mục tiêu:

- biết mỗi executable khởi động cái gì
- biết service nào chạy web API, service nào chạy background worker
- biết DI đang kéo các shared/data layers vào đâu

### 4. Đọc README trong từng project

Đọc:

- [README.md](../src/NotificationSystem.Api/README.md)
- [README.md](../src/NotificationSystem.Processor/README.md)
- [README.md](../src/NotificationSystem.Worker.Email/README.md)
- [README.md](../src/NotificationSystem.Worker.Sms/README.md)
- [README.md](../src/NotificationSystem.Worker.Push/README.md)
- [README.md](../src/NotificationSystem.AuditLogger/README.md)
- [README.md](../src/NotificationSystem.Shared/README.md)
- [README.md](../src/NotificationSystem.Data/README.md)

Mục tiêu:

- có “mental map” rõ ràng cho từng project
- biết mở tiếp file nào nếu muốn đi sâu

## Lộ trình 30-45 phút

### 5. Đọc business flow quan trọng nhất

Đọc theo thứ tự:

1. [Contracts.cs](../src/NotificationSystem.Shared/Models/Contracts.cs)
2. [Enums.cs](../src/NotificationSystem.Shared/Models/Enums.cs)
3. [NotificationProcessingService.cs](../src/NotificationSystem.Shared/Services/NotificationProcessingService.cs)
4. [ChannelDeliveryService.cs](../src/NotificationSystem.Shared/Services/ChannelDeliveryService.cs)
5. [FakeNotificationProviders.cs](../src/NotificationSystem.Shared/Services/FakeNotificationProviders.cs)

Mục tiêu:

- hiểu notification được quyết định `Cancelled`, `Processing`, `Sent`, `DeadLettered` ra sao
- hiểu retry và DLQ flow
- hiểu fake provider đang mô phỏng transient/permanent failure thế nào

### 6. Đọc persistence layer

Đọc:

- [MySqlNotificationRepository.cs](../src/NotificationSystem.Data/Persistence/MySqlNotificationRepository.cs)
- [CassandraAuditRepository.cs](../src/NotificationSystem.Data/Persistence/CassandraAuditRepository.cs)
- [MySqlConnectionFactory.cs](../src/NotificationSystem.Data/Persistence/MySqlConnectionFactory.cs)

Mục tiêu:

- biết dữ liệu operational đang được lưu thế nào
- biết audit trail đang được ghi ra sao
- biết local bootstrap/seed/retry đang đặt ở đâu

### 7. Đọc hạ tầng local

Đọc:

- [docker-compose.yml](../docker-compose.yml)
- [nginx.conf](../deploy/nginx/nginx.conf)
- [ThirdParty-Preparation.md](../docs/ThirdParty-Preparation.md)

Mục tiêu:

- biết local environment được dựng như thế nào
- biết vì sao có `Nginx`, `Kafka`, `MySQL`, `Redis`, `Cassandra`
- biết phase sau cần làm gì để cắm provider thật

## Nếu bạn muốn sửa một loại thay đổi cụ thể

### Thêm field mới vào request notification

Đọc trước:

- [Contracts.cs](../src/NotificationSystem.Shared/Models/Contracts.cs)
- [Program.cs](../src/NotificationSystem.Api/Program.cs)
- [MySqlNotificationRepository.cs](../src/NotificationSystem.Data/Persistence/MySqlNotificationRepository.cs)

### Thay fake provider bằng provider thật

Đọc trước:

- [FakeNotificationProviders.cs](../src/NotificationSystem.Shared/Services/FakeNotificationProviders.cs)
- [ChannelDeliveryService.cs](../src/NotificationSystem.Shared/Services/ChannelDeliveryService.cs)
- [ThirdParty-Preparation.md](../docs/ThirdParty-Preparation.md)

### Sửa logic consent/rate limit

Đọc trước:

- [NotificationProcessingService.cs](../src/NotificationSystem.Shared/Services/NotificationProcessingService.cs)
- [RedisRateLimiter.cs](../src/NotificationSystem.Shared/Services/RedisRateLimiter.cs)
- [MySqlNotificationRepository.cs](../src/NotificationSystem.Data/Persistence/MySqlNotificationRepository.cs)

### Sửa flow retry/DLQ

Đọc trước:

- [ChannelDeliveryService.cs](../src/NotificationSystem.Shared/Services/ChannelDeliveryService.cs)
- [TopicNameResolver.cs](../src/NotificationSystem.Shared/Services/TopicNameResolver.cs)
- [NotificationFlowTests.cs](../tests/NotificationSystem.Tests/NotificationFlowTests.cs)

## Cách đọc nhanh nhất nếu chỉ có 5 phút

Đọc đúng 3 file này:

1. [System-Components-Overview.md](../docs/System-Components-Overview.md)
2. [Program.cs](../src/NotificationSystem.Api/Program.cs)
3. [ChannelDeliveryService.cs](../src/NotificationSystem.Shared/Services/ChannelDeliveryService.cs)

Sau 3 file đó, bạn sẽ hiểu:

- request vào từ đâu
- message chạy qua đâu
- notification kết thúc bằng cách nào

## Cách đọc nhanh nhất nếu sẽ code ngay

Nếu bạn sắp code vào repo, thứ tự khuyên đọc là:

1. [README.md](../README.md)
2. [System-Components-Overview.md](../docs/System-Components-Overview.md)
3. README của đúng project bạn sắp sửa
4. entrypoint `Program.cs` của project đó
5. shared service hoặc repository mà project đó đang dùng
6. test liên quan nếu có

## Tóm tắt

Người mới không nên bắt đầu bằng việc đọc toàn bộ `src/` từ trên xuống.

Thứ tự hợp lý hơn là:

`README repo -> doc tổng quan -> README từng project -> Program.cs -> shared business flow -> persistence -> docker-compose`

Lộ trình này giúp hiểu hệ thống nhanh mà vẫn giữ được bức tranh lớn trước khi đi vào chi tiết.

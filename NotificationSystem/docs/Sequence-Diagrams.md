# Sequence Diagrams

Tài liệu này mô tả các flow chính của notification system bằng Mermaid sequence diagram để dễ nhìn hơn khi onboarding hoặc review kiến trúc.

## 1. Success Flow

Đây là luồng chuẩn khi notification đi hết pipeline và được gửi thành công.

```mermaid
sequenceDiagram
    participant U as "Upstream Service"
    participant N as "Nginx"
    participant A as "Notification API"
    participant R as "Redis"
    participant M as "MySQL"
    participant K as "Kafka"
    participant P as "Processor"
    participant W as "Channel Worker"
    participant C as "Cassandra"

    U->>N: POST /api/notifications
    N->>A: Forward request
    A->>A: Validate JWT
    A->>R: Reserve dedupe key
    A->>M: Insert notification (Pending)
    A->>K: Publish audit Accepted
    A->>K: Publish to priority topic
    A->>M: Update status Queued
    A->>K: Publish audit Queued
    A-->>U: 202 Accepted

    P->>K: Consume priority topic
    P->>M: Load active device setting
    P->>R: Check rate limit
    P->>R: Get blueprint cache
    alt blueprint cache miss
        P->>M: Load blueprint
        P->>R: Cache blueprint
    end
    P->>M: Update status Processing
    P->>K: Publish audit Processing
    P->>K: Publish to channel topic

    W->>K: Consume channel topic
    W->>M: Load active destination
    W->>W: Call fake provider
    W->>M: Update status Sent
    W->>K: Publish audit Delivered

    K->>C: Audit logger stores events
```

## 2. Cancel Flow

Đây là luồng khi notification bị dừng sớm do không có consent hoặc vượt rate limit.

```mermaid
sequenceDiagram
    participant U as "Upstream Service"
    participant N as "Nginx"
    participant A as "Notification API"
    participant M as "MySQL"
    participant R as "Redis"
    participant K as "Kafka"
    participant P as "Processor"
    participant C as "Cassandra"

    U->>N: POST /api/notifications
    N->>A: Forward request
    A->>M: Insert notification
    A->>K: Publish to priority topic
    A-->>U: 202 Accepted

    P->>K: Consume priority topic
    P->>M: Load active device setting
    alt no active device / no consent
        P->>M: Update status Cancelled
        P->>K: Publish audit Cancelled
    else device exists
        P->>R: Check rate limit
        alt rate limit exceeded
            P->>M: Update status Cancelled
            P->>K: Publish audit Cancelled
        end
    end

    K->>C: Audit logger stores events
```

## 3. Retry And DLQ Flow

Đây là luồng khi worker gặp lỗi transient rồi retry, hoặc cuối cùng phải đẩy sang dead-letter queue.

```mermaid
sequenceDiagram
    participant K as "Kafka"
    participant W as "Channel Worker"
    participant M as "MySQL"
    participant P as "Fake Provider"
    participant C as "Cassandra"

    W->>K: Consume channel topic
    W->>M: Load active destination
    W->>P: Deliver notification

    alt transient failure and retry budget remains
        P-->>W: Transient failure
        W->>W: Backoff delay
        W->>M: Update status Failed / increment attempt
        W->>K: Publish audit RetryScheduled
        W->>K: Re-publish to same channel topic
        K->>C: Audit logger stores retry event
    else permanent failure or retries exhausted
        P-->>W: Permanent failure
        W->>M: Update status DeadLettered
        W->>K: Publish to DLQ topic
        W->>K: Publish audit DeadLettered
        K->>C: Audit logger stores dead-letter event
    else success
        P-->>W: Success
        W->>M: Update status Sent
        W->>K: Publish audit Delivered
        K->>C: Audit logger stores delivered event
    end
```

## 4. Cách dùng tài liệu này

Nếu đang onboarding:

- đọc [System-Components-Overview.md](./System-Components-Overview.md) trước
- sau đó xem doc này để chuyển từ “đọc mô tả” sang “nhìn flow”

Nếu đang debug:

- `Success Flow` giúp kiểm tra hệ thống đang kẹt ở API, processor, hay worker
- `Cancel Flow` giúp phân biệt notification bị hủy ở bước consent hay rate limit
- `Retry And DLQ Flow` giúp kiểm tra behavior khi provider lỗi

## 5. Tài liệu liên quan

- [README.md](../README.md)
- [System-Components-Overview.md](./System-Components-Overview.md)
- [Onboarding-Path.md](./Onboarding-Path.md)

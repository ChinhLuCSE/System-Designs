# System Designs

Repo này tổng hợp các bài `System Design` mình tự phân tích, ghi chú, và hiện thực hoá dần theo từng chủ đề.

Mục tiêu của repo:

- Lưu lại tư duy thiết kế hệ thống theo cách dễ đọc, dễ mở rộng.
- Kết hợp giữa phần `design note` và phần `implementation` để nhìn được cả lý thuyết lẫn cách triển khai thực tế.
- Cập nhật dần thêm các system mới theo thời gian.

## Repo này có gì?

Mỗi system thường sẽ gồm một hoặc nhiều phần sau:

- `System design document`: phân tích yêu cầu, trade-offs, API, data model, scaling strategy, bottlenecks.
- `Implementation`: code minh hoạ hoặc bản triển khai nâng cao cho design đó.
- `Diagrams / images`: hình minh hoạ cho luồng xử lý hoặc kiến trúc.
- `Run locally`: hướng dẫn chạy local bằng Docker hoặc stack phù hợp.

## Danh sách system hiện có

| System | Nội dung | Trạng thái |
| --- | --- | --- |
| [TinyURL](./TinyURL) | URL shortener với design note + advanced implementation bằng .NET, Cassandra, Redis, ZooKeeper, Nginx | Available |
| [NotificationSystem](./NotificationSystem) |  | Unavailable |

## Cách đọc repo

Nếu bạn muốn đi từ tổng quan đến implementation, thứ tự hợp lý là:

1. Đọc `README.md` trong thư mục của system.
2. Xem file `*-System-Design.md` để nắm requirement, data model, API, và scaling decisions.
3. Chạy implementation local để quan sát flow thực tế.

## Định hướng update tiếp

Repo này sẽ được update dần với nhiều bài system design khác như:

- Rate Limiter
- Notification System
- Chat System
- File Storage / CDN
- News Feed

Danh sách trên chỉ là định hướng, không phải roadmap cố định.

## Lưu ý

- Một số implementation trong repo được tối ưu để demo concept và trade-off, chưa phải production-ready hoàn toàn.
- Mỗi bài có thể ưu tiên một số khía cạnh khác nhau: scalability, consistency, caching, queuing, observability, hoặc deployability.

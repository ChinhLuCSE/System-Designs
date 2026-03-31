# TinyURL Advanced Implementation

Advanced implementation cho bài toán TinyURL với stack:

- .NET 10 Minimal API
- Cassandra cho persistent storage
- Redis cho read cache
- ZooKeeper cho cấp phát range ID theo block
- Nginx làm load balancer trước 2 API instances
- Docker Compose để chạy local nhanh

System design: [TinyURL-System-Design.md](/Users/ADMIN/Desktop/Projects/System-Designs/TinyURL/TinyURL-System-Design.md)

## Kiến trúc

`POST /create-url`

1. Request đi vào Nginx.
2. Nginx phân phối về một trong hai API instances.
3. API xin ID từ ZooKeeper theo block `1,000,000` giá trị mỗi range.
4. ID được encode Base62 thành `shortCode`.
5. Mapping được ghi vào Cassandra và warm cache ở Redis.

`GET /{shortCode}`

1. Request đi qua Nginx.
2. API đọc Redis trước.
3. Nếu cache miss thì đọc Cassandra.
4. API trả về `301 Permanent Redirect`.

## Chạy nhanh

```bash
docker compose up --build
```

Sau khi stack sẵn sàng:

- API/LB: [http://localhost:8080](http://localhost:8080)
- Health: [http://localhost:8080/health](http://localhost:8080/health)

## Test nhanh

Tạo short URL:

```bash
curl -X POST http://localhost:8080/create-url \
  -H "Content-Type: application/json" \
  -d "{\"longUrl\":\"https://learn.microsoft.com/aspnet/core\"}"
```

Response mẫu:

```json
{
  "id": "0195ee2b-8573-7ddf-a72f-a109b70a8f13",
  "longUrl": "https://learn.microsoft.com/aspnet/core",
  "shortCode": "0000001",
  "shortUrl": "http://localhost:8080/0000001",
  "createdAtUtc": "2026-03-31T03:00:00.0000000+00:00"
}
```

Redirect:

```bash
curl -i http://localhost:8080/0000001
```

## Postman

Import 2 file sau vào Postman:

- [TinyURL-Advanced.postman_collection.json](/Users/ADMIN/Desktop/Projects/System-Designs/TinyURL/postman/TinyURL-Advanced.postman_collection.json)
- [TinyURL-Local.postman_environment.json](/Users/ADMIN/Desktop/Projects/System-Designs/TinyURL/postman/TinyURL-Local.postman_environment.json)

Flow dùng nhanh:

1. Chọn environment `TinyURL Local`.
2. Chạy `Health Check`.
3. Chạy `Create URL`.
4. Chạy `Resolve Short URL`.

Request `Create URL` sẽ tự lưu `shortCode` mới vào collection variable để request redirect dùng lại ngay.

## Ghi chú

- Compose hiện dùng `1` Cassandra node và `1` ZooKeeper node để local demo nhanh. Khi production có thể scale các tầng này theo topology riêng.
- ZooKeeper chỉ bị gọi khi instance cần range mới, nên giảm rất mạnh tần suất đồng bộ so với cấp số tăng dần theo từng request.
- Redis dùng policy `allkeys-lru` để bám sát phần caching trong design.

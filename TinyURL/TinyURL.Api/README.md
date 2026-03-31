# TinyURL.Api Backend Flow

`TinyURL.Api` là backend server của hệ thống TinyURL. Service này expose 2 API chính:

- `POST /create-url`: tạo short URL từ một long URL.
- `GET /{shortCode}`: resolve short code và redirect sang URL gốc.

## 1. Thành phần chính

- `Program.cs`: cấu hình DI, options, rate limit, health check và map endpoint.
- `ShortUrlService`: orchestration chính cho luồng create và resolve.
- `ZooKeeperRangeAllocator`: cấp phát ID theo từng range để nhiều instance không bị đụng nhau.
- `Base62Encoder`: chuyển numeric ID thành short code base62.
- `CassandraUrlRepository`: lưu và đọc mapping `short_code -> long_url`.
- `RedisUrlCache`: cache mapping để tăng tốc resolve.

## 2. Luồng khởi động server

Khi app start:

1. ASP.NET Core bind các section cấu hình `TinyUrl`, `Cassandra`, `Redis`, `ZooKeeper`.
2. Đăng ký các dependency singleton cho repository, cache, range allocator và service chính.
3. Bật `ProblemDetails`, `HealthChecks`, `OpenAPI` (chỉ ở môi trường development) và rate limiter cho API tạo short URL.
4. Gọi `InitializeInfrastructureAsync()` trước khi nhận request:
   - `CassandraUrlRepository.InitializeAsync()`:
     - kết nối cluster Cassandra
     - tạo keyspace nếu chưa có
     - chọn keyspace
     - tạo table `urls` nếu chưa có
     - prepare sẵn câu lệnh `INSERT` và `SELECT`
   - `ZooKeeperRangeAllocator.WarmupAsync()`:
     - đảm bảo các node `/tinyurl` và `/tinyurl/range-counter` tồn tại
     - lấy trước một range ID để sẵn sàng generate short code

Mục tiêu của bước warmup là để request đầu tiên không phải tự khởi tạo toàn bộ hạ tầng.

## 3. Luồng tạo short URL

Endpoint: `POST /create-url`

### Bước 1: Validate request

Server chỉ chấp nhận `LongUrl` là absolute URL và có scheme `http` hoặc `https`.

Nếu không hợp lệ, API trả về `ValidationProblem`.

### Bước 2: Lấy ID mới từ ZooKeeper

`ShortUrlService.CreateAsync()` gọi `IRangeAllocator.GetNextIdAsync()`.

`ZooKeeperRangeAllocator` hoạt động như sau:

1. Giữ một range ID trong memory, ví dụ `1 -> 1_000_000`.
2. Mỗi lần tạo URL mới, service chỉ lấy số tiếp theo trong range hiện tại.
3. Khi range hết, service dùng ZooKeeper để atomically tăng giá trị counter `/tinyurl/range-counter`.
4. Instance nào update counter thành công sẽ sở hữu range mới.

Thiết kế này giúp:

- không phải gọi ZooKeeper cho từng request
- giảm lock phân tán
- tránh trùng ID giữa nhiều backend instance

### Bước 3: Encode ID thành short code

ID số được đưa vào `Base62Encoder.Encode()` để đổi sang bảng ký tự:

- `0-9`
- `a-z`
- `A-Z`

Nếu code ngắn hơn `TinyUrl:MinimumCodeLength` thì sẽ được pad bên trái bằng `0`.

### Bước 4: Tạo record domain

Service tạo `ShortUrlRecord` gồm:

- `Id`: `Guid v7`
- `LongUrl`
- `ShortCode`
- `CreatedAtUtc`

Lưu ý:

- `shortCode` được sinh từ numeric ID
- `Guid v7` chủ yếu dùng làm metadata/identifier cho response và record

### Bước 5: Persist xuống Cassandra

`CassandraUrlRepository.SaveAsync()` ghi dữ liệu vào Cassandra với primary key là `short_code`.

Schema hiện tại:

- `short_code text PRIMARY KEY`
- `id uuid`
- `long_url text`
- `created_at timestamp`

Consistency level khi ghi là `Quorum`.

### Bước 6: Ghi cache vào Redis

Sau khi save thành công, service ghi luôn cặp:

- key: `tinyurl:{shortCode}`
- value: `longUrl`

TTL mặc định lấy từ `Redis:DefaultTtl`, hoặc fallback sang `TinyUrl:CacheTtlHours`.

### Bước 7: Trả response

API trả về `201 Created` với:

- metadata của record
- `ShortUrl` dạng `{PublicBaseUrl}/{shortCode}`

## 4. Luồng resolve short URL

Endpoint: `GET /{shortCode}`

### Bước 1: Đọc cache trước

`ShortUrlService.ResolveAsync()` gọi `RedisUrlCache.GetAsync(shortCode)`.

Nếu cache hit:

- trả về long URL ngay
- API response là redirect `301`

### Bước 2: Cache miss thì đọc Cassandra

Nếu Redis không có dữ liệu:

1. Service query Cassandra theo `short_code`
2. Nếu không có record:
   - trả về `404 Not Found`
3. Nếu có record:
   - lấy `long_url`
   - set lại cache Redis
   - trả redirect `301`

Luồng này tối ưu cho case đọc nhiều hơn ghi nhiều, vì request resolve thường lớn hơn request create.

## 5. Rate limiting và reliability

### Rate limiting

`POST /create-url` đang dùng fixed window limiter:

- tối đa `100` request / phút
- không queue request

Điều này giúp bảo vệ luồng generate ID và ghi storage khỏi bị spam quá mức.

### Retry

Cả `CassandraUrlRepository` và `ZooKeeperRangeAllocator` đều có retry với exponential backoff đơn giản khi:

- kết nối hạ tầng lỗi tạm thời
- ZooKeeper bị conflict version khi nhiều instance cùng xin range

## 6. Tóm tắt luồng end-to-end

### Create flow

```text
Client
  -> POST /create-url
  -> validate URL
  -> ZooKeeper lấy numeric ID
  -> Base62 encode thành short code
  -> Cassandra lưu mapping
  -> Redis set cache
  -> trả short URL
```

### Resolve flow

```text
Client
  -> GET /{shortCode}
  -> Redis get
  -> hit: redirect 301
  -> miss: Cassandra query
  -> có dữ liệu: Redis set -> redirect 301
  -> không có dữ liệu: 404
```

## 7. Một vài lưu ý thiết kế

- Hệ thống hiện chưa kiểm tra duplicate theo `long_url`, nên cùng một URL gốc có thể tạo ra nhiều short code khác nhau.
- Cache là write-through ở luồng create, và cache-aside ở luồng resolve.
- Dữ liệu nguồn chân lý vẫn là Cassandra; Redis chỉ là lớp tối ưu đọc.
- ZooKeeper chỉ giữ counter/range allocation, không lưu mapping URL.
- Redirect đang dùng `permanent: true`, tương ứng HTTP `301`.

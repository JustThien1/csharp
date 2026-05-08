# Sơ đồ Sequence - App (Bổ sung)

> Các chức năng đã có file PNG riêng: Geofence, load map POI, QR code scan, đăng ký tài khoản, đăng nhập.  
> File này bổ sung các chức năng còn thiếu.

---

## 1. Xem chi tiết POI

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant API as POIController
    participant DB as AppDbContext

    User->>App: Bấm vào POI trên bản đồ hoặc danh sách
    App->>API: GET /api/poi/{id} (Bearer token)
    API->>DB: Query POI by Id (JOIN Category, Audio, Review)
    DB-->>API: POI chi tiết + danh sách Audio + Reviews
    API-->>App: 200 OK + poiDetailDto
    App->>App: Hiển thị màn hình chi tiết POI
    App->>App: Hiển thị: Tên, Mô tả, Địa chỉ, Hình ảnh, Rating trung bình
    App->>App: Hiển thị danh sách audio (theo ngôn ngữ)
    App->>App: Hiển thị khoảng cách từ vị trí hiện tại

    alt Người dùng là Premium hoặc audio miễn phí
        App->>App: Hiển thị nút "Nghe thuyết minh"
    else Người dùng chưa có gói Premium
        App->>App: Hiển thị nút "Nâng cấp để nghe"
    end
```

---

## 2. Phát audio thuyết minh (thủ công)

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant AudioSvc as AudioService (local)
    participant API as AudioController
    participant DB as AppDbContext
    participant Player as Plugin.Maui.Audio

    User->>App: Bấm nút "Nghe thuyết minh" trên POI
    App->>API: GET /api/audio?poiId={id}&language={lang} (Bearer token)
    API->>DB: Query Audio WHERE PoiId = id AND Language = lang AND IsActive = true
    DB-->>API: Audio record (AudioUrl, DurationSeconds)
    API-->>App: 200 OK + audioDto

    App->>Player: PlayAsync(audioUrl)
    Player->>Player: Stream / download audio từ URL
    Player-->>App: Phát audio

    App->>App: Hiển thị thanh điều khiển (Play/Pause, tiến trình)
    App->>App: Hiển thị tên POI, thời lượng

    Note over App,DB: Ghi nhận lịch sử nghe
    App->>API: POST /api/playback/log {poiId, audioId, durationListened} (Bearer token)
    API->>DB: INSERT PlaybackLog
    DB-->>API: OK
    API-->>App: 200 OK

    alt Người dùng tạm dừng
        User->>App: Bấm Pause
        App->>Player: Pause()
        Player-->>App: Đã dừng
    else Người dùng tiếp tục
        User->>App: Bấm Play
        App->>Player: Resume()
        Player-->>App: Tiếp tục phát
    else Audio kết thúc
        Player-->>App: Sự kiện PlaybackEnded
        App->>App: Ẩn thanh điều khiển hoặc gợi ý POI tiếp theo
    end
```

---

## 3. Yêu thích POI (Favorites)

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant API as FavouriteController
    participant DB as AppDbContext

    Note over User,DB: --- Thêm yêu thích ---
    User->>App: Bấm icon trái tim trên POI
    App->>API: POST /api/favourite {poiId} (Bearer token)
    API->>DB: Kiểm tra đã yêu thích chưa
    alt Chưa yêu thích
        API->>DB: INSERT Favourite (UserId, PoiId)
        DB-->>API: OK
        API-->>App: 201 Created
        App->>App: Đổi icon trái tim thành màu đỏ (đã thích)
    else Đã yêu thích rồi (toggle off)
        API->>DB: DELETE Favourite WHERE UserId = id AND PoiId = poiId
        DB-->>API: OK
        API-->>App: 200 OK
        App->>App: Đổi icon trái tim về màu xám (bỏ thích)
    end

    Note over User,DB: --- Xem danh sách yêu thích ---
    User->>App: Vào tab "Yêu thích"
    App->>API: GET /api/favourite (Bearer token)
    API->>DB: Query Favourites JOIN POIs WHERE UserId = currentUser
    DB-->>API: Danh sách POI yêu thích
    API-->>App: 200 OK + list
    App->>App: Hiển thị danh sách POI đã thích
```

---

## 4. Đánh giá POI (Reviews)

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant API as ReviewController
    participant DB as AppDbContext

    Note over User,DB: --- Xem đánh giá ---
    User->>App: Vào mục "Đánh giá" trong chi tiết POI
    App->>API: GET /api/review?poiId={id}
    API->>DB: Query Reviews JOIN Users WHERE PoiId = id ORDER BY CreatedAt DESC
    DB-->>API: Review list
    API-->>App: 200 OK + list
    App->>App: Hiển thị danh sách đánh giá (Rating, Comment, Tên người dùng)

    Note over User,DB: --- Gửi đánh giá ---
    User->>App: Bấm "Viết đánh giá"
    App->>App: Hiển thị form: chọn số sao (1-5) + nhập comment
    User->>App: Chọn sao, nhập nội dung, bấm "Gửi"
    App->>API: POST /api/review {poiId, rating, comment} (Bearer token)
    API->>DB: Kiểm tra đã đánh giá POI này chưa
    alt Chưa đánh giá
        API->>DB: INSERT Review (UserId, PoiId, Rating, Comment)
        DB-->>API: OK
        API-->>App: 201 Created + reviewDto
        App->>App: Thêm đánh giá vào danh sách, cập nhật rating trung bình
    else Đã đánh giá rồi
        API-->>App: 409 Conflict "Bạn đã đánh giá POI này rồi"
        App->>App: Hiển thị thông báo lỗi
    end
```

---

## 5. Xem lịch sử nghe (Playback History)

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant API as PlaybackController
    participant DB as AppDbContext

    User->>App: Vào tab "Lịch sử"
    App->>API: GET /api/playback/history (Bearer token)
    API->>DB: Query PlaybackLogs JOIN POIs JOIN Audios WHERE UserId = currentUser ORDER BY PlayedAt DESC
    DB-->>API: Lịch sử nghe (POI name, Audio language, DurationListened, PlayedAt)
    API-->>App: 200 OK + list
    App->>App: Hiển thị danh sách lịch sử theo ngày

    User->>App: Bấm vào một mục lịch sử
    App->>App: Chuyển đến màn hình chi tiết POI tương ứng
```

---

## 6. Nhận và xem thông báo (Notifications)

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant API as NotificationController
    participant DB as AppDbContext

    Note over User,DB: --- Khi mở app / vào tab thông báo ---
    User->>App: Vào tab "Thông báo"
    App->>API: GET /api/notification (Bearer token)
    API->>DB: Query Notifications WHERE UserId = currentUser ORDER BY CreatedAt DESC
    DB-->>API: Notification list (Title, Body, IsRead, CreatedAt)
    API-->>App: 200 OK + list
    App->>App: Hiển thị danh sách thông báo
    App->>App: Thông báo chưa đọc hiển thị badge / in đậm

    Note over User,DB: --- Đánh dấu đã đọc ---
    User->>App: Bấm vào thông báo
    App->>API: PUT /api/notification/{id}/read (Bearer token)
    API->>DB: UPDATE Notification set IsRead = true
    DB-->>API: OK
    API-->>App: 200 OK
    App->>App: Cập nhật UI (bỏ in đậm, giảm badge count)
    App->>App: Hiển thị nội dung thông báo hoặc điều hướng đến POI liên quan
```

---

## 7. Thanh toán / Gia hạn gói Premium

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant API as PaymentController
    participant DB as AppDbContext
    participant Pay as Payment Gateway (VNPay/Momo)

    User->>App: Vào trang "Nâng cấp Premium"
    App->>App: Hiển thị các gói (1 tháng, 3 tháng, 1 năm) và giá

    User->>App: Chọn gói và bấm "Thanh toán"
    App->>API: POST /api/payment/create {packageType, amount} (Bearer token)
    API->>DB: INSERT Payment (Status = Pending)
    DB-->>API: Payment record
    API->>Pay: Tạo phiên thanh toán (orderId, amount, returnUrl)
    Pay-->>API: Payment URL
    API-->>App: 200 OK + {paymentUrl, paymentId}

    App->>App: Mở WebView hoặc redirect đến trang thanh toán của cổng
    User->>Pay: Nhập thông tin thẻ / quét QR / xác nhận thanh toán
    Pay-->>App: Callback kết quả (success/failed) qua deeplink

    alt Thanh toán thành công
        App->>API: POST /api/payment/confirm {paymentId, providerRef} (Bearer token)
        API->>Pay: Verify giao dịch
        Pay-->>API: Confirmed
        API->>DB: UPDATE Payment status = Success
        API->>DB: UPDATE User SubscriptionExpiresAt += duration
        DB-->>API: OK
        API-->>App: 200 OK + {newExpiresAt}
        App->>App: Hiển thị "Thanh toán thành công!"
        App->>App: Cập nhật trạng thái Premium trong app
    else Thanh toán thất bại
        App->>API: POST /api/payment/confirm {paymentId, status=failed}
        API->>DB: UPDATE Payment status = Failed
        DB-->>API: OK
        API-->>App: 200 OK
        App->>App: Hiển thị "Thanh toán thất bại, vui lòng thử lại"
    end
```

---

## 8. Đăng xuất

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant LocalDB as SQLite Local
    participant API as AuthController

    User->>App: Vào Settings, bấm "Đăng xuất"
    App->>App: Hiển thị dialog xác nhận
    User->>App: Xác nhận đăng xuất

    App->>API: POST /api/auth/logout (Bearer token)
    API-->>App: 200 OK

    App->>LocalDB: Xóa token lưu trong local storage
    App->>LocalDB: Xóa thông tin người dùng cache
    LocalDB-->>App: OK

    App->>App: Dừng các background service (Geofence, Location tracking)
    App->>App: Chuyển về màn hình Đăng nhập
```

---

## 9. Cập nhật vị trí và ghi Route Log

```mermaid
sequenceDiagram
    participant App as MAUI App
    participant GeoSvc as Shiny.Locations
    participant API as RouteController
    participant DB as AppDbContext

    Note over App,DB: Chạy liên tục khi app ở foreground / background
    App->>GeoSvc: Đăng ký lắng nghe vị trí (StartListening)
    GeoSvc-->>App: Sự kiện LocationChanged (Lat, Lng, Accuracy)

    loop Mỗi khi vị trí thay đổi đáng kể (> 10m)
        App->>API: POST /api/route/log {lat, lng, timestamp} (Bearer token)
        API->>DB: INSERT RouteLog (UserId, Lat, Lng, Timestamp)
        DB-->>API: OK
        API-->>App: 200 OK
    end

    App->>GeoSvc: StopListening (khi đăng xuất hoặc tắt app)
```

---

## 10. Tìm kiếm POI

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant API as POIController
    participant DB as AppDbContext

    User->>App: Gõ từ khóa vào thanh tìm kiếm
    App->>App: Debounce 300ms để tránh gọi API quá nhiều

    App->>API: GET /api/poi/search?q={keyword}&lat={lat}&lng={lng} (Bearer token)
    API->>DB: Query POIs WHERE Name LIKE '%keyword%' OR Address LIKE '%keyword%'
    API->>API: Tính khoảng cách từ (lat, lng) đến từng POI
    API->>API: Sắp xếp theo khoảng cách gần nhất
    DB-->>API: POI list
    API-->>App: 200 OK + filtered + sorted list

    App->>App: Hiển thị kết quả tìm kiếm
    User->>App: Bấm vào kết quả
    App->>App: Chuyển đến màn hình chi tiết POI
```

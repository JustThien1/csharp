# Sơ đồ Hoạt động (Activity Diagram) - App

---

## 1. Khởi động ứng dụng

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[App khởi động]
    B --> C[Kiểm tra token trong LocalStorage]
    C --> D{Token tồn tại?}
    D -- Không --> E[Chuyển đến màn hình Đăng nhập]
    D -- Có --> F[Gọi GET /api/auth/me để xác minh token]
    F --> G{Token hợp lệ?}
    G -- Không / Hết hạn --> H[Xóa token]
    H --> E
    G -- Có --> I[Tải thông tin User: tên, role, gói subscription]
    I --> J[Tải cấu hình app: ngôn ngữ, preferences]
    J --> K[Khởi động Location Service - Shiny.Locations]
    K --> L[Khởi động Geofence Monitoring]
    L --> M[Chuyển đến màn hình chính - Bản đồ]
    M --> N([Kết thúc])
    E --> N
```

---

## 2. Đăng ký tài khoản

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Mở màn hình Đăng ký]
    B --> C[Nhập FullName]
    C --> D[Nhập Email]
    D --> E[Nhập Phone]
    E --> F[Nhập Password]
    F --> G[Nhập xác nhận Password]
    G --> H{Validation phía client}
    H -- Email sai định dạng --> D
    H -- Password không khớp --> F
    H -- Thiếu trường bắt buộc --> C
    H -- Hợp lệ --> I[POST /api/auth/register]
    I --> J{Phản hồi từ API}
    J -- 409 Email đã tồn tại --> K[Hiển thị lỗi email đã dùng]
    K --> D
    J -- 400 Lỗi khác --> L[Hiển thị thông báo lỗi]
    L --> C
    J -- 201 Thành công --> M[Hiển thị thông báo đăng ký thành công]
    M --> N[Tự động chuyển sang Đăng nhập]
    N --> O([Kết thúc])
```

---

## 3. Đăng nhập

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Mở màn hình Đăng nhập]
    B --> C[Nhập Username / Email]
    C --> D[Nhập Password]
    D --> E{Trường rỗng?}
    E -- Có --> F[Hiển thị cảnh báo]
    F --> C
    E -- Không --> G[POST /api/auth/login]
    G --> H{Kết quả?}
    H -- 401 Sai thông tin --> I[Hiển thị lỗi sai tài khoản / mật khẩu]
    I --> C
    H -- 403 Bị khóa --> J[Hiển thị thông báo tài khoản bị khóa]
    J --> K([Kết thúc])
    H -- 200 Thành công --> L[Lưu JWT token vào SecureStorage]
    L --> M[Lưu thông tin User vào bộ nhớ]
    M --> N[Khởi động Location Service]
    N --> O[Chuyển đến màn hình Bản đồ chính]
    O --> K
```

---

## 4. Xem bản đồ và chọn POI

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Hiển thị bản đồ TP.HCM]
    B --> C[GET /api/poi - Tải danh sách POI]
    C --> D[Render các marker POI lên bản đồ]
    D --> E[Hiển thị khoảng cách từ vị trí hiện tại đến từng POI]

    E --> F{Người dùng tương tác}

    F -- Di chuyển bản đồ --> G[Cập nhật vị trí hiển thị]
    G --> F

    F -- Bấm vào Marker POI --> H[Hiển thị popup thông tin nhanh]
    H --> I[Tên POI, khoảng cách, rating]
    I --> J{Bấm Xem chi tiết?}
    J -- Có --> K[Mở màn hình Chi tiết POI]
    J -- Không --> F

    F -- Tìm kiếm --> L[Nhập từ khóa]
    L --> M[GET /api/poi/search?q=keyword]
    M --> N[Highlight kết quả trên bản đồ]
    N --> F

    F -- Lọc theo danh mục --> O[Chọn Category]
    O --> P[GET /api/poi?categoryId=]
    P --> Q[Cập nhật markers]
    Q --> F

    K --> R([Kết thúc])
```

---

## 5. Nghe thuyết minh (thủ công)

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Người dùng ở màn hình Chi tiết POI]
    B --> C{Kiểm tra quyền truy cập}
    C -- Chưa đăng nhập --> D[Yêu cầu đăng nhập]
    D --> E([Kết thúc])
    C -- Chưa Premium & audio yêu cầu Premium --> F[Hiện thông báo Nâng cấp]
    F --> G{Muốn nâng cấp?}
    G -- Có --> H[Chuyển đến màn hình Thanh toán]
    G -- Không --> E
    C -- Có quyền --> I[Hiển thị danh sách audio theo ngôn ngữ]
    I --> J[Người dùng chọn ngôn ngữ vi/en]
    J --> K[GET /api/audio?poiId=&language=]
    K --> L{Tìm thấy audio?}
    L -- Không --> M[Hiển thị Chưa có audio cho ngôn ngữ này]
    M --> I
    L -- Có --> N[Bắt đầu phát audio - Plugin.Maui.Audio]
    N --> O[Hiển thị thanh điều khiển: Play/Pause, tiến trình, thời lượng]

    O --> P{Người dùng thao tác}
    P -- Pause --> Q[Tạm dừng phát]
    Q --> R[Người dùng bấm Play] --> N
    P -- Tua --> S[Seek đến vị trí mới]
    S --> N
    P -- Dừng --> T[Stop audio]
    P -- Audio kết thúc --> T

    T --> U[POST /api/playback/log - Ghi lịch sử nghe]
    U --> V([Kết thúc])
```

---

## 6. Geofence tự động phát audio

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[App đang chạy foreground/background]
    B --> C[Shiny.Locations theo dõi vị trí GPS liên tục]
    C --> D{Vị trí thay đổi > 5m?}
    D -- Không --> C
    D -- Có --> E[So sánh vị trí với danh sách POI đã tải]
    E --> F{Cách POI nào < bán kính Geofence?}
    F -- Không --> G[POST /api/route/log - Ghi vị trí]
    G --> C
    F -- Có --> H{POI này đã phát gần đây chưa?}
    H -- Đã phát trong 30 phút --> C
    H -- Chưa --> I{App đang phát audio khác?}
    I -- Đang phát --> J[Thêm POI vào hàng đợi]
    J --> C
    I -- Không phát --> K[Hiển thị thông báo: Bạn đang ở gần POI X]
    K --> L[Tự động phát audio thuyết minh của POI]
    L --> M[Hiển thị màn hình Now Playing]
    M --> N[POST /api/playback/log - Ghi lịch sử]
    N --> O[Đánh dấu POI đã phát - tránh phát lại liền]
    O --> C
```

---

## 7. Quét QR Code

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Người dùng bấm nút Quét QR]
    B --> C{Quyền camera?}
    C -- Chưa cấp --> D[Yêu cầu quyền camera]
    D --> E{Người dùng cho phép?}
    E -- Không --> F[Hiển thị thông báo cần quyền camera]
    F --> G([Kết thúc])
    E -- Có --> H[Mở camera - QR Scanner]
    C -- Đã cấp --> H

    H --> I[Camera quét frame liên tục]
    I --> J{Phát hiện QR Code?}
    J -- Không --> I
    J -- Có --> K[Đọc nội dung QR - URL/deeplink]
    K --> L{Đây là QR của TourGuide?}
    L -- Không --> M[Hiển thị thông báo QR không hợp lệ]
    M --> I
    L -- Có --> N[Parse poiId từ URL]
    N --> O[GET /api/poi/:id]
    O --> P{Tìm thấy POI?}
    P -- Không --> Q[Hiển thị POI không tồn tại]
    Q --> G
    P -- Có --> R[Mở màn hình Chi tiết POI]
    R --> S[Tự động bắt đầu phát audio]
    S --> T[POST /api/playback/log]
    T --> G
```

---

## 8. Yêu thích, Đánh giá và Lịch sử

```mermaid
flowchart TD
    A([Bắt đầu]) --> B{Người dùng chọn chức năng}

    B -- Yêu thích POI --> C[Bấm icon tim trên màn hình POI]
    C --> D{Đã yêu thích chưa?}
    D -- Chưa --> E[POST /api/favourite]
    E --> F[Icon tim đổi sang màu đỏ]
    D -- Rồi --> G[DELETE /api/favourite/:id]
    G --> H[Icon tim đổi sang xám]
    F --> I[Cập nhật tab Yêu thích]
    H --> I

    B -- Xem danh sách yêu thích --> J[Vào tab Yêu thích]
    J --> K[GET /api/favourite]
    K --> L[Hiển thị danh sách POI đã thích]
    L --> M{Chọn POI?}
    M -- Có --> N[Mở chi tiết POI]

    B -- Đánh giá POI --> O[Bấm Viết đánh giá trong chi tiết POI]
    O --> P[Chọn số sao 1-5]
    P --> Q[Nhập nội dung nhận xét]
    Q --> R[POST /api/review]
    R --> S{Đã đánh giá trước đó?}
    S -- Có --> T[Hiển thị đã đánh giá rồi]
    S -- Không --> U[Lưu review - Cập nhật rating POI]
    U --> V[Hiển thị review mới trong danh sách]

    B -- Lịch sử nghe --> W[Vào tab Lịch sử]
    W --> X[GET /api/playback/history]
    X --> Y[Hiển thị danh sách theo ngày giảm dần]
    Y --> Z{Bấm vào mục?}
    Z -- Có --> AA[Mở chi tiết POI]

    I --> BB([Kết thúc])
    N --> BB
    T --> BB
    V --> BB
    AA --> BB
```

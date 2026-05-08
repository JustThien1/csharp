# Sơ đồ Hoạt động (Activity Diagram) - Admin

---

## 1. Quản lý địa điểm POI

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin vào trang Quản lý POI]
    B --> C[Xem danh sách POI]
    C --> D{Chọn hành động}

    D -- Tạo mới --> E[Mở form tạo POI]
    E --> F[Nhập thông tin: Tên, Mô tả, Địa chỉ, Toạ độ, Ảnh, Danh mục]
    F --> G{Dữ liệu hợp lệ?}
    G -- Không --> F
    G -- Có --> H[Gửi POST /api/poi]
    H --> I[Lưu vào DB - Status: Pending]
    I --> J[Cập nhật danh sách]

    D -- Sửa --> K[Chọn POI cần sửa]
    K --> L[Mở form với dữ liệu hiện tại]
    L --> M[Chỉnh sửa thông tin]
    M --> N{Dữ liệu hợp lệ?}
    N -- Không --> M
    N -- Có --> O[Gửi PUT /api/poi/:id]
    O --> P[Cập nhật DB]
    P --> J

    D -- Xóa --> Q[Hiện dialog xác nhận]
    Q --> R{Xác nhận?}
    R -- Không --> C
    R -- Có --> S[Gửi DELETE /api/poi/:id]
    S --> T{Có ràng buộc?}
    T -- Có --> U[Hiển thị lỗi]
    U --> C
    T -- Không --> V[Xóa khỏi DB]
    V --> J

    D -- Duyệt POI --> W[Xem danh sách POI chờ duyệt]
    W --> X{Quyết định}
    X -- Duyệt --> Y[PUT approve]
    X -- Từ chối --> Z[PUT reject + nhập lý do]
    Y --> AA[Cập nhật Status = Approved]
    Z --> AB[Cập nhật Status = Rejected]
    AA --> AC[Gửi thông báo cho người tạo]
    AB --> AC
    AC --> J

    J --> AD([Kết thúc])
```

---

## 2. Quản lý người dùng

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin vào trang Quản lý Người dùng]
    B --> C[GET /api/users - Tải danh sách]
    C --> D[Hiển thị bảng người dùng]
    D --> E{Chọn hành động}

    E -- Tạo mới --> F[Mở form tạo user]
    F --> G[Nhập FullName, Email, Phone, Role, Password]
    G --> H{Email trùng?}
    H -- Có --> I[Hiển thị lỗi email]
    I --> G
    H -- Không --> J[POST /api/users]
    J --> K[Lưu user vào DB]
    K --> L[Cập nhật danh sách]

    E -- Sửa thông tin --> M[Chọn user]
    M --> N[Mở form với dữ liệu hiện tại]
    N --> O[Chỉnh sửa thông tin]
    O --> P[PUT /api/users/:id]
    P --> Q[Cập nhật DB]
    Q --> L

    E -- Khóa / Mở khóa --> R[Chọn user]
    R --> S{Trạng thái hiện tại?}
    S -- Đang hoạt động --> T[PUT toggle - IsActive = false]
    S -- Đang bị khóa --> U[PUT toggle - IsActive = true]
    T --> V[Gửi thông báo tài khoản bị khóa]
    U --> W[Gửi thông báo tài khoản được mở]
    V --> L
    W --> L

    E -- Xóa --> X[Hiện dialog xác nhận]
    X --> Y{Xác nhận?}
    Y -- Không --> D
    Y -- Có --> Z[DELETE /api/users/:id]
    Z --> L

    E -- Lọc / Tìm kiếm --> AA[Nhập từ khóa hoặc chọn Role]
    AA --> AB[GET /api/users?search=&role=]
    AB --> AC[Cập nhật bảng kết quả]
    AC --> E

    L --> AD([Kết thúc])
```

---

## 3. Tạo audio thuyết minh (TTS)

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin vào trang Quản lý Audio]
    B --> C[Chọn POI cần tạo audio]
    C --> D[Mở AudioDialog - Tab TTS]
    D --> E[Nhập nội dung văn bản thuyết minh]
    E --> F[Chọn ngôn ngữ vi/en]
    F --> G[Chọn giọng đọc Nam/Nữ]
    G --> H[Chọn tốc độ đọc]
    H --> I{Văn bản hợp lệ?}
    I -- Không --> E
    I -- Có --> J[POST /api/audio/tts - Gửi yêu cầu TTS]
    J --> K[INSERT TtsJob status=Pending]
    K --> L[Hiển thị thông báo đang xử lý]

    L --> M{Background worker}
    M --> N[Lấy job Pending từ DB]
    N --> O[Gọi Google Cloud TTS API]
    O --> P{TTS thành công?}
    P -- Không --> Q[UPDATE TtsJob status=Failed]
    Q --> R[Hiển thị lỗi cho Admin]
    P -- Có --> S[Lưu file audio vào Storage]
    S --> T[INSERT Audio record vào DB]
    T --> U[UPDATE TtsJob status=Done]
    U --> V[Audio xuất hiện trong danh sách]

    R --> W([Kết thúc])
    V --> W
```

---

## 4. Xem Heatmap

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin vào trang Heatmap]
    B --> C[Chọn khoảng thời gian lọc]
    C --> D[GET /api/analytics/heatmap?from=&to=]
    D --> E[Truy vấn PlaybackLogs và RouteLogs]
    E --> F[Tổng hợp điểm nóng theo Lat/Lng]
    F --> G[Trả về danh sách điểm với tần suất]
    G --> H[Render bản đồ Leaflet.js]
    H --> I[Vẽ Heatmap layer lên bản đồ]
    I --> J[Hiển thị bản đồ nhiệt]

    J --> K{Admin muốn lọc thêm?}
    K -- Có --> L[Thay đổi bộ lọc ngày / loại dữ liệu]
    L --> D
    K -- Không --> M{Admin muốn xuất?}
    M -- Có --> N[Export dữ liệu CSV / ảnh PNG]
    N --> O([Kết thúc])
    M -- Không --> O
```

---

## 5. Xem lịch sử nghe (Playback History)

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin vào trang Lịch sử nghe]
    B --> C[GET /api/playback/history?page=1]
    C --> D[Query PlaybackLogs JOIN Users JOIN POIs]
    D --> E[Hiển thị bảng lịch sử]

    E --> F{Admin muốn lọc?}
    F -- Theo user --> G[Chọn user từ dropdown]
    G --> H[GET /api/playback/history?userId=]
    F -- Theo POI --> I[Nhập tên POI]
    I --> J[GET /api/playback/history?poiId=]
    F -- Theo ngày --> K[Chọn khoảng thời gian]
    K --> L[GET /api/playback/history?from=&to=]
    H --> M[Cập nhật bảng]
    J --> M
    L --> M

    M --> N{Xem chi tiết?}
    N -- Có --> O[Hiển thị modal: POI name, audio language, thời lượng nghe, thời điểm]
    O --> P{Điều hướng đến POI?}
    P -- Có --> Q[Mở trang chi tiết POI]
    P -- Không --> M
    N -- Không --> R([Kết thúc])
```

---

## 6. Quản lý tour và tạo QR Code

```mermaid
flowchart TD
    A([Bắt đầu]) --> B{Admin chọn chức năng}

    B -- Quản lý Tour --> C[Vào trang Tour]
    C --> D[GET /api/tour - Tải danh sách]
    D --> E[Hiển thị danh sách tour]
    E --> F{Hành động}

    F -- Tạo mới --> G[Nhập tên tour, mô tả]
    G --> H[Chọn danh sách POI theo thứ tự]
    H --> I[POST /api/tour]
    I --> J[Lưu Tour + TourPOIs vào DB]
    J --> E

    F -- Sửa --> K[Chọn tour]
    K --> L[Chỉnh sửa thông tin + thêm bớt POI]
    L --> M[PUT /api/tour/:id]
    M --> N[Cập nhật DB]
    N --> E

    F -- Xóa --> O[Xác nhận xóa]
    O --> P[DELETE /api/tour/:id]
    P --> E

    B -- Tạo QR Code --> Q[Vào trang QR Code]
    Q --> R[GET /api/poi - Tải danh sách POI]
    R --> S[Hiển thị dropdown chọn POI]
    S --> T[Admin chọn POI]
    T --> U[Tạo URL deeplink: tourguide://poi/:id]
    U --> V[Render QR Code từ URL bằng thư viện client]
    V --> W[Hiển thị preview QR Code]
    W --> X{Hành động}
    X -- Tải xuống --> Y[Export QR thành file PNG]
    X -- In --> Z[Mở print dialog trình duyệt]
    X -- Chọn POI khác --> T

    Y --> AA([Kết thúc])
    Z --> AA
```

---

## 7. Theo dõi tuyến di chuyển người dùng

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin vào trang Route Tracking]
    B --> C[GET /api/users - Tải danh sách user]
    C --> D[Hiển thị dropdown chọn user]
    D --> E[Admin chọn user]
    E --> F[Chọn khoảng thời gian]
    F --> G[GET /api/route?userId=&from=&to=]
    G --> H[Query RouteLogs ORDER BY Timestamp ASC]
    H --> I{Có dữ liệu?}
    I -- Không --> J[Hiển thị thông báo không có dữ liệu]
    J --> D
    I -- Có --> K[Trả về mảng điểm: Lat, Lng, Timestamp]
    K --> L[Render bản đồ Leaflet.js]
    L --> M[Vẽ polyline lộ trình trên bản đồ]
    M --> N[Đánh dấu điểm đầu cuối hành trình]
    N --> O[Hiển thị các POI đã ghé thăm dọc theo tuyến]

    O --> P{Admin muốn so sánh?}
    P -- Có --> Q[Chọn thêm user khác]
    Q --> R[Vẽ thêm polyline màu khác]
    R --> O
    P -- Không --> S{Xuất báo cáo?}
    S -- Có --> T[Export dữ liệu lộ trình CSV]
    T --> U([Kết thúc])
    S -- Không --> U
```

---

## 8. Quản lý audio (xem, sửa, xóa, khôi phục)

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[Admin vào trang Audio]
    B --> C[GET /api/audio - Tải danh sách]
    C --> D[Hiển thị bảng audio với bộ lọc POI / ngôn ngữ]
    D --> E{Hành động}

    E -- Upload mới --> F[Mở AudioDialog]
    F --> G{Chọn phương thức}
    G -- TTS --> H[Nhập text, chọn giọng / tốc độ]
    H --> I[POST /api/audio/tts]
    G -- Upload file --> J[Chọn file mp3/wav ≤20MB]
    J --> K[POST /api/audio/upload multipart]
    G -- Nhập URL --> L[Nhập URL audio]
    L --> M[POST /api/audio với audioUrl]
    I --> N[Cập nhật danh sách]
    K --> N
    M --> N

    E -- Nghe thử --> O[Bấm nút play trên dòng audio]
    O --> P[Phát audio từ AudioUrl trong trình duyệt]
    P --> D

    E -- Sửa metadata --> Q[Chọn audio cần sửa]
    Q --> R[Sửa Description, Language, IsActive]
    R --> S[PUT /api/audio/:id]
    S --> T[Cập nhật DB]
    T --> N

    E -- Xóa --> U[Xác nhận xóa]
    U --> V[DELETE /api/audio/:id]
    V --> W[Xóa khỏi DB + Storage]
    W --> N

    E -- Khôi phục --> X[Vào trang Audio Recovery]
    X --> Y[GET /api/audio/recovery/orphaned]
    Y --> Z[Hiển thị audio mồ côi/lỗi]
    Z --> AA{Hành động khôi phục}
    AA -- Gán lại POI --> AB[Chọn POI, PUT reassign]
    AA -- Xóa hẳn --> AC[DELETE audio lỗi]
    AB --> N
    AC --> N

    N --> AD([Kết thúc])
```

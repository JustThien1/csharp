# Sơ đồ Chuyển đổi Ngôn ngữ (Tiếng Việt / Tiếng Anh) - App

---

## 1. Sequence: Người dùng đổi ngôn ngữ trong Settings

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant Pref as Preferences (Local)
    participant API as POIController
    participant Player as AudioPlayerService

    User->>App: Vào Settings → chọn Ngôn ngữ
    App->>App: Hiển thị lựa chọn: Tiếng Việt / English

    alt Chọn Tiếng Việt
        User->>App: Chọn "Tiếng Việt"
        App->>Pref: Save("app_language", "vi")
    else Chọn English
        User->>App: Chọn "English"
        App->>Pref: Save("app_language", "en")
    end

    Pref-->>App: OK

    App->>App: Cập nhật CultureInfo toàn ứng dụng
    App->>App: Re-render UI: nhãn, thông báo, placeholder theo ngôn ngữ mới

    Note over Player: Nếu đang phát audio → dừng
    App->>Player: StopAsync() (nếu đang phát)
    Player-->>App: Stopped

    App->>App: Ghi nhớ preferredAudioLanguage = language mới

    Note over App,API: Không cần gọi lại API ngay,\naudio language áp dụng lần mở POI tiếp theo
    App-->>User: Hiển thị thông báo "Đã đổi sang Tiếng Việt / English"
```

---

## 2. Sequence: Load audio theo ngôn ngữ khi mở chi tiết POI

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant Pref as Preferences (Local)
    participant API as AudioController
    participant DB as AppDbContext
    participant Player as AudioPlayerService

    User->>App: Mở màn hình Chi tiết POI (poiId = X)

    App->>Pref: Get("app_language") → "vi" hoặc "en"
    Pref-->>App: preferredLang

    App->>API: GET /api/audio?poiId=X&language={preferredLang}
    API->>DB: SELECT * FROM Audios WHERE PoiId=X AND Language=preferredLang AND IsActive=true
    DB-->>API: Kết quả

    alt Tìm thấy audio
        API-->>App: 200 OK + AudioDto (AudioUrl, DurationSeconds, Description)
        App->>App: Hiển thị nút "▶ Nghe thuyết minh" (active)
        App->>App: Hiển thị ngôn ngữ hiện tại: 🇻🇳 VI / 🇺🇸 EN

        User->>App: Bấm nút phát
        App->>Player: PlayAsync(audioUrl)
        Player-->>App: Đang phát
        App->>App: Hiển thị thanh điều khiển + tiến trình

    else Không tìm thấy audio ngôn ngữ này
        API-->>App: 200 OK + [] (rỗng) hoặc 404
        App->>App: Hiển thị "Chưa có thuyết minh bằng {preferredLang}"

        App->>API: GET /api/audio?poiId=X (không lọc language)
        API->>DB: SELECT DISTINCT Language FROM Audios WHERE PoiId=X AND IsActive=true
        DB-->>API: Danh sách ngôn ngữ có sẵn
        API-->>App: ["vi"] hoặc ["en"] hoặc []

        alt Có ngôn ngữ thay thế
            App->>App: Hiển thị gợi ý: "Có thuyết minh bằng {alt_lang} - Nghe thử?"
            User->>App: Chấp nhận chuyển sang ngôn ngữ thay thế
            App->>API: GET /api/audio?poiId=X&language={alt_lang}
            API-->>App: AudioDto
            App->>Player: PlayAsync(audioUrl)
        else Không có audio nào
            App->>App: Hiển thị "POI này chưa có thuyết minh"
            App->>App: Ẩn nút phát
        end
    end
```

---

## 3. Sequence: Chuyển ngôn ngữ audio ngay trong màn hình POI

```mermaid
sequenceDiagram
    participant User as Người dùng
    participant App as MAUI App
    participant API as AudioController
    participant DB as AppDbContext
    participant Player as AudioPlayerService
    participant Pref as Preferences (Local)

    Note over App: Đang ở màn hình Chi tiết POI
    Note over App: Đang hiển thị audio "vi" (hoặc "en")

    App->>App: Hiển thị tab chọn ngôn ngữ: [🇻🇳 VI] [🇺🇸 EN]
    User->>App: Bấm tab ngôn ngữ khác (ví dụ: EN)

    alt Đang phát audio ngôn ngữ cũ
        App->>Player: StopAsync()
        Player-->>App: Stopped
    end

    App->>API: GET /api/audio?poiId={currentPoiId}&language=en
    API->>DB: SELECT * FROM Audios WHERE PoiId=X AND Language='en' AND IsActive=true
    DB-->>API: Kết quả

    alt Có audio tiếng Anh
        API-->>App: 200 OK + AudioDto
        App->>App: Cập nhật UI: tab EN active, hiện thông tin audio mới
        App->>App: Hiển thị nút "▶ Listen to Narration"
        User->>App: Bấm Play
        App->>Player: PlayAsync(audioUrl_en)
        Player-->>App: Đang phát tiếng Anh
        App->>Pref: Save("preferred_audio_lang_poi_{id}", "en")
    else Không có audio tiếng Anh
        API-->>App: [] rỗng
        App->>App: Tab EN bị mờ (disabled)
        App->>App: Hiển thị tooltip: "No English narration available"
        App->>App: Giữ nguyên tab VI đang chọn
    end
```

---

## 4. Activity: Luồng hoạt động chọn ngôn ngữ audio

```mermaid
flowchart TD
    A([Bắt đầu]) --> B[App khởi động]
    B --> C[Đọc app_language từ Preferences]
    C --> D{Có preference?}
    D -- Không --> E[Lấy ngôn ngữ thiết bị\nDeviceInfo.CurrentCulture]
    E --> F{Thiết bị dùng tiếng Việt?}
    F -- Có --> G[Đặt mặc định: vi]
    F -- Không --> H[Đặt mặc định: en]
    D -- Có --> I[Dùng preference đã lưu]
    G --> J[preferredLang = vi/en]
    H --> J
    I --> J

    J --> K[Người dùng mở màn hình Chi tiết POI]
    K --> L[GET /api/audio?poiId=X&language=preferredLang]
    L --> M{Audio tồn tại?}

    M -- Có --> N[Hiển thị nút Phát - active]
    N --> O{Người dùng muốn đổi ngôn ngữ?}
    O -- Không --> P[Phát audio ngôn ngữ hiện tại]
    O -- Có --> Q[Bấm tab ngôn ngữ khác]
    Q --> R[GET audio ngôn ngữ mới]
    R --> S{Có audio ngôn ngữ mới?}
    S -- Có --> T[Cập nhật player - phát audio mới]
    S -- Không --> U[Hiển thị tab disabled + tooltip]
    U --> O
    T --> V[Lưu lựa chọn ngôn ngữ audio cho POI này]

    M -- Không --> W[Hiển thị cảnh báo không có audio]
    W --> X[GET danh sách ngôn ngữ có sẵn cho POI]
    X --> Y{Có ngôn ngữ thay thế?}
    Y -- Có --> Z[Đề xuất chuyển sang ngôn ngữ thay thế]
    Z --> AA{Người dùng đồng ý?}
    AA -- Có --> R
    AA -- Không --> AB[Hiển thị màn hình POI không có audio]
    Y -- Không --> AB

    P --> AC([Kết thúc])
    V --> AC
    AB --> AC
```

---

## 5. State Diagram: Trạng thái ngôn ngữ trong App

```mermaid
stateDiagram-v2
    [*] --> KhoiDong : App khởi động

    KhoiDong --> DocPreference : Đọc cài đặt ngôn ngữ
    DocPreference --> NgonNguViet : preference = "vi"\nhoặc device locale = vi
    DocPreference --> NgonNguAnh : preference = "en"\nhoặc device locale khác

    state NgonNguViet {
        [*] --> UIViet : Render giao diện tiếng Việt
        UIViet --> AudioViet : Mở POI → load audio "vi"
        AudioViet --> DangPhatViet : User bấm Play
        DangPhatViet --> DungViet : User Pause
        DungViet --> DangPhatViet : User Resume
        DangPhatViet --> [*] : Audio kết thúc
    }

    state NgonNguAnh {
        [*] --> UIAnh : Render giao diện English
        UIAnh --> AudioAnh : Mở POI → load audio "en"
        AudioAnh --> DangPhatAnh : User bấm Play
        DangPhatAnh --> DungAnh : User Pause
        DungAnh --> DangPhatAnh : User Resume
        DangPhatAnh --> [*] : Audio kết thúc
    }

    NgonNguViet --> NgonNguAnh : User đổi sang English\n(Settings hoặc tab POI)
    NgonNguAnh --> NgonNguViet : User đổi sang Tiếng Việt\n(Settings hoặc tab POI)

    NgonNguViet --> [*] : App đóng - lưu preference "vi"
    NgonNguAnh --> [*] : App đóng - lưu preference "en"
```

---

## 6. Sequence: Tự động phát Geofence theo ngôn ngữ ưa thích

```mermaid
sequenceDiagram
    participant Geo as GeofenceService
    participant Pref as Preferences (Local)
    participant API as AudioController
    participant DB as AppDbContext
    participant Player as AudioPlayerService
    participant App as MAUI App

    Geo->>Geo: Phát hiện người dùng vào vùng POI (poiId = Y)
    Geo->>Pref: Get("app_language") → preferredLang
    Pref-->>Geo: "vi" hoặc "en"

    Geo->>API: GET /api/audio?poiId=Y&language={preferredLang}
    API->>DB: Query Audio
    DB-->>API: Kết quả

    alt Có audio đúng ngôn ngữ
        API-->>Geo: AudioDto
        Geo->>Player: PlayAsync(audioUrl)
        Player-->>App: Phát audio (ngôn ngữ ưa thích)
        App->>App: Hiển thị thông báo: "🎧 Đang phát thuyết minh [POI Name]"
    else Không có audio ngôn ngữ ưa thích
        API-->>Geo: []
        Geo->>API: GET /api/audio?poiId=Y (fallback - không lọc language)
        API->>DB: Query bất kỳ audio nào IsActive
        DB-->>API: AudioDto (ngôn ngữ thay thế)

        alt Có audio ngôn ngữ khác
            API-->>Geo: AudioDto (ngôn ngữ thay thế)
            Geo->>Player: PlayAsync(audioUrl_fallback)
            App->>App: Hiển thị: "🎧 Phát thuyết minh [POI] (tiếng {fallback_lang})"
        else Không có audio nào
            Geo->>App: Thông báo im lặng (không phát)
        end
    end

    Geo->>API: POST /api/playback/log {poiId, audioId, triggeredBy: "geofence"}
    API->>DB: INSERT PlaybackLog
    DB-->>API: OK
```

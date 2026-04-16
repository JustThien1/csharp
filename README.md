# 🗺️ TourGuide HCM

Ứng dụng hướng dẫn du lịch TP.HCM — nghe thuyết minh tự động khi đến gần địa điểm.

**Sinh viên:** Nguyễn Chí Thiện — 3123411275  
**GVHD:** ThS. Nguyễn Quốc Huy  
**Môn:** Ngôn ngữ lập trình C# — Đại học Sài Gòn

---

## Gồm 3 phần

| Project | Mô tả | Port |
|---|---|---|
| `TourGuideHCM.API` | Backend ASP.NET Core 8 + SQLite | 5284 |
| `TourGuideHCM.Admin` | Trang quản trị Blazor WebAssembly | 5063 |
| `TourGuideHCM.App` | App Android .NET MAUI 8 | — |

---

## Tính năng

**App Android:**
- Bản đồ với các địa điểm POI, hiển thị khoảng cách
- Tự động phát thuyết minh khi vào vùng bán kính (Geofencing)
- Quét QR code tại điểm dừng để nghe thuyết minh
- Đăng nhập / đăng ký tài khoản
- Hỗ trợ Tiếng Việt, English, Tiếng Trung, Tiếng Hàn

**Trang quản trị:**
- Quản lý địa điểm POI, audio, tour
- Tạo giọng đọc từ văn bản (Google Cloud TTS)
- Xem tuyến di chuyển của người dùng trên bản đồ
- Heatmap mức độ nghe tại từng địa điểm
- Lịch sử nghe, quản lý người dùng
- Tạo mã QR cho app

---

## Chạy dự án

### 1. Cấu hình API

Thêm vào `TourGuideHCM.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=tourguide.db"
  },
  "Jwt": {
    "Key": "your-secret-key-minimum-32-characters"
  },
  "GoogleTTS": {
    "ApiKey": "your-google-tts-api-key"
  }
}
```

### 2. Migrate database

```bash
cd TourGuideHCM.API
dotnet ef database update
```

### 3. Chạy

Chạy cả `TourGuideHCM.API` và `TourGuideHCM.Admin` cùng lúc trong Visual Studio (Multiple Startup Projects).

Deploy app lên emulator hoặc điện thoại thật qua USB.

> **Điện thoại thật:** Đổi `BaseUrl` trong `ApiService.cs` thành IP LAN của máy (VD: `http://192.168.0.6:5284`). Máy tính và điện thoại phải cùng WiFi.

---

## Công nghệ

- **App:** .NET MAUI 8, C#, SQLite, Plugin.Maui.Audio, Shiny.Locations
- **Admin:** Blazor WebAssembly, MudBlazor, Leaflet.js
- **API:** ASP.NET Core 8, Entity Framework Core, SQLite, JWT, Google Cloud TTS


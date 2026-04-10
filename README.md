Nội dung 
-	Chức năng cần thiết (PoC)
-	Chức năng mở rộng 
-	Kiến trúc gợi ý
1. GPS tracking theo thời gian thực
- Lấy vị trí người dùng liên tục (breground + background)
- Tối ưu pin, độ chính xác 
2. Geofence/ kích hoạt điểm thuyết minh
* Thiết lập điểm POI ( point of interest) với:
-Tọa độ
-Bán kính kích hoạt
-mục tiêu ưu tiên
*Tự động phát nội dung khi người dùng:
- Đi vào vùng
- Đến gần điểm 
*Có hệ thống chống spam = debounce và cooldown
3.Thuyết minh tự động
*Text to speech (TTS)
-Linh hoạt, đa ngôn ngữ 
- Dung lượng nhẹ
*File audio thu sẵn 
-Giọng tự nhiên, chuyên nghiệp
-Chất lượng cao, nhưng nặng dữ liệu
4.Quản lý dữ liệu POI
- Danh sách điểm thuyết minh
- Mô tả văn bản 
- Ảnh thuyết họa
- Linh bản 
5. Map view 
- Hiển thị vị trí người dùng trên bản đồ
- Hiển thị tất cả POI
-Highlight POI đăng gần nhất
-Xem chi tiết POI

MVP 
1.Hệ thống quản trị nội dung (CMS)
-Tạo trang web quản lý
-POI
-Audio
-Bản dịch
-Lịch sử sử dụng
-Quản lý tour
2.Phân tích dữ liệu(Analytics)
-Lưu tuyến di chuyển (ẩn danh)
-Top địa điểm được nghe nhiều nhất
-Thời gian trung bình nghe POI
-Heat map vị trí người dùng
3.QR code kích hoạt nội dung
- Dùng tại điểm dừng xe buýt các phường
4. Luộng hoạt động mẫu
-App tải danh sách POI(lat/ling),bán kính, ưu tiên, nội dung thuyết minh




1️⃣ BFD – Block Flow Diagram của hệ thống
                ┌────────────────────────┐
                │        Người dùng      │
                │      (Mobile App)      │
                └───────────┬────────────┘
                            │
                            ▼
                ┌────────────────────────┐
                │     Mobile Application │
                │       (.NET MAUI)      │
                └───────────┬────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼

┌───────────────┐   ┌────────────────┐   ┌─────────────────┐
│ GPS Tracking  │   │ Map View       │   │ QR Code Scanner │
│ - Lấy vị trí  │   │ - Hiển thị map │   │ - Quét QR       │
│ - Background  │   │ - POI gần nhất │   │ - Kích hoạt POI │
└───────┬───────┘   └────────┬───────┘   └────────┬────────┘
        │                    │                    │
        └──────────────┬─────┴───────┬────────────┘
                       ▼             ▼

              ┌────────────────────────┐
              │ Geofence Engine        │
              │ - Kiểm tra bán kính    │
              │ - Kích hoạt POI        │
              │ - Cooldown / debounce  │
              └───────────┬────────────┘
                          │
                          ▼

              ┌────────────────────────┐
              │ Thuyết minh tự động    │
              │                        │
              │ - Text To Speech      │
              │ - Audio thu sẵn       │
              └───────────┬────────────┘
                          │
                          ▼

              ┌────────────────────────┐
              │ Backend API            │
              │ (ASP.NET Core)         │
              └───────────┬────────────┘
                          │
            ┌─────────────┼─────────────┐
            ▼                           ▼

  ┌───────────────────┐        ┌────────────────────┐
  │ Database          │        │ CMS Website        │
  │                   │        │ (Quản trị hệ thống)│
  │ - POI             │        │                    │
  │ - Audio           │        │ - Quản lý POI      │
  │ - Bản dịch        │        │ - Upload Audio     │
  │ - User Logs       │        │ - Quản lý Tour     │
  │ - Analytics       │        │ - Xem thống kê     │
  └───────────────────┘        └────────────────────┘
________________________________________
2️⃣ Các module chính của hệ thống
1. Mobile App
Công nghệ: C# (.NET MAUI)
Module:
Mobile App
│
├── GPS Tracking
├── Geofence Manager
├── Map View
├── Audio Player
├── Text To Speech
└── QR Code Scanner
________________________________________
2. Backend Server
Backend API
│
├── POI API
├── Location Tracking API
├── Audio API
├── Translation API
└── Analytics API
________________________________________
3. CMS Website
Admin CMS
│
├── Quản lý POI
├── Upload audio
├── Quản lý bản dịch
├── Quản lý tour
└── Thống kê người dùng
________________________________________
________________________________________
4️⃣ Luồng hoạt động chính của hệ thống
User mở app
      │
      ▼
App lấy GPS
      │
      ▼
Kiểm tra khoảng cách với POI
      │
      ▼
Nếu vào vùng Geofence
      │
      ▼
Kích hoạt thuyết minh
      │
      ▼
Phát Audio hoặc TTS
      │
      ▼
Ghi log vào server
________________________________________
5️⃣ Các địa điểm du lịch TP.HCM bạn có thể dùng làm POI
Ví dụ:
•	Ben Thanh Market
•	Independence Palace
•	Saigon Notre-Dame Basilica
•	Saigon Central Post Office
•	Nguyen Hue Walking Street
________________________________________
6️⃣ Kiến trúc công nghệ đề xuất (dễ làm đồ án)
Thành phần	Công nghệ
Mobile App	.NET MAUI
Backend	ASP.NET Core
CMS	ASP.NET MVC
Database	SQL Server
Map	Google Maps API
QR Code	ZXing







TourGuideHCM
│
├───────────────────────────────
│ 1. BACKEND (ASP.NET CORE API)
│───────────────────────────────
│
├── TourGuideHCM.API
│
│ ├── Controllers
│ │ ├── POIController.cs
│ │ ├── TourController.cs
│ │ ├── UserController.cs
│ │ ├── AudioController.cs ← (NEW)
│ │ └── AnalyticsController.cs ← (NEW)
│ │
│ ├── Models
│ │ ├── POI.cs
│ │ ├── Tour.cs
│ │ ├── User.cs
│ │ ├── Audio.cs ← (NEW)
│ │ └── TrackingLog.cs ← (NEW)
│ │
│ ├── DTOs
│ │ ├── POIDTO.cs
│ │ ├── TourDTO.cs
│ │ ├── AudioDTO.cs ← (NEW)
│ │ └── TrackingDTO.cs ← (NEW)
│ │
│ ├── Services
│ │ ├── POIService.cs
│ │ ├── AudioService.cs
│ │ ├── GeofenceService.cs ← (NEW - xử lý logic khoảng cách)
│ │ └── AnalyticsService.cs ← (NEW)
│ │
│ ├── Repositories
│ │ ├── IPOIRepository.cs
│ │ ├── POIRepository.cs
│ │ ├── IAudioRepository.cs ← (NEW)
│ │ └── AudioRepository.cs ← (NEW)
│ │
│ ├── Data
│ │ ├── AppDbContext.cs
│ │ └── DbSeeder.cs
│ │
│ ├── Helpers
│ │ ├── HaversineHelper.cs ← (NEW - tính khoảng cách GPS)
│ │ └── FileHelper.cs ← (NEW)
│ │
│ ├── wwwroot
│ │ ├── images
│ │ └── audio
│ │
│ ├── appsettings.json
│ ├── Program.cs
│ └── TourGuideHCM.API.csproj
│
├───────────────────────────────
│ 2. MOBILE APP (.NET MAUI)
│───────────────────────────────
│
├── TourGuideHCM.Mobile
│
│ ├── Views
│ │ ├── MapPage.xaml
│ │ ├── POIDetailPage.xaml
│ │ └── SettingsPage.xaml
│ │
│ ├── ViewModels
│ │ ├── MapViewModel.cs
│ │ └── POIViewModel.cs
│ │
│ ├── Services
│ │ ├── ApiService.cs
│ │ ├── GPSService.cs
│ │ ├── GeofenceService.cs
│ │ ├── AudioService.cs
│ │ └── TTSService.cs
│ │
│ ├── Models
│ │ ├── POIModel.cs
│ │ └── TourModel.cs
│ │
│ ├── Helpers
│ │ └── DistanceHelper.cs
│ │
│ └── App.xaml
│
├───────────────────────────────
│ 3. CMS WEBSITE (ADMIN)
│───────────────────────────────
│
└── TourGuideHCM.Web
│
├── pages
│ ├── poi.html
│ ├── tour.html
│ └── dashboard.html
│
├── js
│ ├── api.js
│ ├── poi.js
│ └── analytics.js
│
├── css
│ └── style.css
│
└── index.html
cd TourGuideHCM.Admin 
dotnet add package MudBlazor
 dotnet add package Microsoft.AspNetCore.Components.WebAssembly.Authentication
 dotnet add package Refit
dotnet add package MudBlazor --version 6.11.2

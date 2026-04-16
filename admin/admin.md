# 📘 TourGuideHCM.Admin - Tài liệu Code Toàn bộ Hệ thống

## 📑 Mục lục
1. [Tổng quan kiến trúc](#tổng-quan-kiến-trúc)
2. [Models (DTO)](#models-dto)
3. [Services](#services)
4. [UI Components (Razor)](#ui-components-razor)
5. [Luồng tương tác (Sequence Diagram)](#luồng-tương-tác)

---

## 🏗️ Tổng quan kiến trúc

TourGuideHCM.Admin là một ứng dụng Blazor WebAssembly với ASP.NET Core 8, MudBlazor UI, và Service-based architecture.

---

## 📦 Models (DTO)

### 1. AudioDto.cs
- Id, PoiId, PoiName, Language, AudioUrl, DurationSeconds, Description, IsActive, FileName

### 2. PoiDto.cs
- Id, Name, Description, Address, Lat, Lng, ImageUrl, CategoryId

### 3. UserDto.cs
- Id, FullName, Email, Phone, Role (UserAdminGuide), IsActive, CreatedDate, TotalListens

### 4. DashboardDto.cs
- TotalPoi, TotalUsers, TopPoi, AvgTime, TopPois, DailyViews

---

## 🔧 Services

### PoiService
- GetAll() → GET apipoi
- Create(PoiDto) → POST apipoi
- Update(PoiDto) → PUT apipoi{id}
- Delete(int id) → DELETE apipoi{id}

### AudioService
- GetAllAsync() → GET apiaudio
- GetByPoiIdAsync(poiId) → GET apiaudiopoi{poiId}
- UploadAudioAsync(content) → POST apiaudioupload
- CreateAsync(audio) → POST apiaudio
- UpdateAsync(audio) → PUT apiaudio{id}
- DeleteAsync(id) → DELETE apiaudio{id}
- LogPlayback(userId, poiId, durationSeconds) → POST apiplayback

### UserService
- GetAllAsync() → GET apiusers
- CreateAsync(user) → POST apiusers
- UpdateAsync(user) → PUT apiusers{id}
- DeleteAsync(id) → DELETE apiusers{id}
- ToggleActiveAsync(id) → PUT apiusers{id}toggle-active

### AnalyticsService
- GetDashboard() → GET apianalyticsdashboard

---

## 🎨 UI Components

### Audio.razor
- Hiển thị danh sách audio guide
- Tìm kiếm real-time theo POIngôn ngữ
- Nghe preview audio trong bảng
- ThêmSửaXóa audio

### AudioDialog.razor
- Tab 1 TTS (Text-to-Speech)
  - Nhập nội dung, chọn languagegenderspeed
  - Convert & Lưu ngay → apiaudioconvert
- Tab 2 Upload File
  - Upload file audio (mp3, wav, m4a, ogg, aac, max 20MB)
  - Nhập URL trực tiếp
  - Lưu metadata vào DB

---

## 📊 Sequence Diagram

```mermaid
sequenceDiagram
  participant UI as Admin UI
  participant Svc as Services
  participant Http as HttpClient
  participant API as Backend API
  participant DB as Database
  participant Stor as Storage

  UI-Svc Yêu cầu dữ liệu
  Svc-Http GET api...
  Http-API Request
  API-DB Query
  DB--API Result
  API--Http Response (JSON)
  Http--Svc Deserialize
  Svc--UI Update UI

  UI-UI Upload file audio
  UI-Http POST apiaudioupload (multipart)
  Http-API Upload
  API-Stor Save file
  Stor--API File URL
  API-DB Save metadata
  DB--API OK
  API--Http Response
  Http--UI Success
```

---

## ⚙️ Setup

Program.cs DI
- AddMudServices()
- HttpClient (BaseAddress localhost5284)
- PoiService, AudioService, UserService, AnalyticsService, PlaybackService

# TourGuideHCM — Data Dictionary

Bảng mô tả chi tiết mọi cột của `schema.sql`. Tham khảo kèm ERD `31_database_schema_detailed`.

## 1. Users
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | INT IDENTITY | No | PK |
| Username | NVARCHAR(100) | No | UNIQUE, dùng để đăng nhập |
| PasswordHash | NVARCHAR(255) | No | BCrypt hash |
| Email | NVARCHAR(255) | No | UNIQUE |
| FullName | NVARCHAR(200) | Yes | Tên hiển thị |
| Role | NVARCHAR(20) | No | `Admin` / `Saler` / `User` |
| IsActive | BIT | No | FALSE = bị khoá |
| CreatedAt / UpdatedAt | DATETIME2 | No / Yes | UTC |

## 2. Categories
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | INT IDENTITY | No | PK |
| Name | NVARCHAR(100) | No | UNIQUE |
| Description | NVARCHAR(500) | Yes | |
| IconUrl | NVARCHAR(255) | Yes | URL icon |
| IsActive | BIT | No | |
| CreatedAt | DATETIME2 | No | |

## 3. POIs
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | INT IDENTITY | No | PK |
| Name | NVARCHAR(200) | No | Tên POI |
| Address | NVARCHAR(500) | Yes | Địa chỉ hiển thị |
| Lat / Lng | FLOAT | No | Toạ độ, có CHECK biên |
| Radius | FLOAT | No | Bán kính geofence (m) |
| NarrationText | NVARCHAR(MAX) | Yes | Nội dung thuyết minh nguồn |
| CategoryId | INT FK | No | → Categories.Id |
| CreatedByUserId | INT FK | Yes | → Users.Id (null khi seed) |
| ReviewStatus | NVARCHAR(20) | No | `Draft`/`Pending`/`Approved`/`Rejected`/`Locked` |
| RejectionReason | NVARCHAR(500) | Yes | Lý do admin reject |
| MergedIntoId | INT FK self | Yes | POI giữ lại sau merge duplicate |
| IsActive | BIT | No | |
| IsLocked | BIT | No | Saler không sửa được |
| CreatedAt / UpdatedAt | DATETIME2 | No / Yes | |

Index: `IX_POIs_ReviewStatus`, `IX_POIs_LatLng` (filtered, IsActive=1).

## 4. Audios
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | INT IDENTITY | No | PK |
| PoiId | INT FK | No | → POIs.Id |
| Language | NVARCHAR(10) | No | `vi`/`en` |
| AudioUrl | NVARCHAR(500) | No | Đường dẫn file |
| FileName | NVARCHAR(255) | Yes | Tên file gốc |
| DurationSeconds | INT | Yes | |
| SizeBytes | BIGINT | Yes | |
| Source | NVARCHAR(20) | No | `Upload`/`TTS` |
| IsActive | BIT | No | |
| CreatedAt | DATETIME2 | No | |

UNIQUE (PoiId, Language, IsActive) — mỗi POI chỉ 1 audio active / ngôn ngữ.

## 5. TtsJobs
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | INT IDENTITY | No | PK |
| PoiId | INT FK | No | → POIs.Id |
| Language | NVARCHAR(10) | No | |
| InputText | NVARCHAR(MAX) | No | Text gửi Google TTS |
| Status | NVARCHAR(20) | No | `Pending`/`Processing`/`Done`/`Failed` |
| AudioUrl | NVARCHAR(500) | Yes | Kết quả |
| ErrorMessage | NVARCHAR(1000) | Yes | |
| RequestedByUserId | INT FK | No | → Users.Id |
| CreatedAt / CompletedAt | DATETIME2 | No / Yes | |

## 6. DuplicateReports
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | INT IDENTITY | No | PK |
| PoiAId | INT FK | No | → POIs.Id |
| PoiBId | INT FK | No | → POIs.Id (khác A) |
| Level | NVARCHAR(20) | No | `High`/`Medium`/`Low` |
| Score | FLOAT | No | 0..1 |
| Status | NVARCHAR(20) | No | `Open`/`Merged`/`Dismissed` |
| ResolvedByUserId | INT FK | Yes | → Users.Id |
| ResolutionNote | NVARCHAR(500) | Yes | |
| CreatedAt / ResolvedAt | DATETIME2 | No / Yes | |

UNIQUE (PoiAId, PoiBId) — 1 cặp POI chỉ tồn tại 1 report.

## 7. Notifications
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | INT IDENTITY | No | PK |
| UserId | INT FK | No | Người nhận |
| Type | NVARCHAR(50) | No | `POI_Approved`, `POI_Rejected`, `Duplicate`, `TTS_Done`, ... |
| Title | NVARCHAR(200) | No | |
| Message | NVARCHAR(1000) | Yes | |
| RelatedEntityId / RelatedEntityType | INT / NVARCHAR(50) | Yes | Tham chiếu sang entity khác |
| IsRead | BIT | No | |
| CreatedAt / ReadAt | DATETIME2 | No / Yes | |

## 8. PlaybackLogs
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | BIGINT IDENTITY | No | PK (dễ lớn, nên dùng BIGINT) |
| UserId | INT FK | Yes | Null với khách không đăng nhập |
| DeviceId | NVARCHAR(100) | No | Dùng để phân biệt khi ẩn danh |
| PoiId | INT FK | No | |
| AudioId | INT FK | Yes | |
| Language | NVARCHAR(10) | Yes | |
| DurationSeconds | INT | No | Thời gian thực tế nghe |
| Completed | BIT | No | Nghe hết bài hay không |
| PlayedAt | DATETIME2 | No | |

## 9. RouteLogs
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | BIGINT IDENTITY | No | PK |
| UserId | INT FK | Yes | |
| DeviceId | NVARCHAR(100) | No | |
| Lat / Lng | FLOAT | No | |
| Accuracy | FLOAT | Yes | mét |
| Speed | FLOAT | Yes | m/s |
| RecordedAt | DATETIME2 | No | thời điểm đo |

## 10. UserHeartbeats
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | INT IDENTITY | No | PK |
| UserId | INT FK | Yes | |
| DeviceId | NVARCHAR(100) | No | UNIQUE — 1 device = 1 hàng |
| LastLat / LastLng | FLOAT | Yes | vị trí gần nhất |
| LastSeen | DATETIME2 | No | dùng để tính "đang online" |

## 11. FavoritePOIs
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | INT IDENTITY | No | PK |
| UserId | INT FK | No | |
| PoiId | INT FK | No | |
| CreatedAt | DATETIME2 | No | |

UNIQUE (UserId, PoiId).

## 12. Tours / TourStops
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Tours.Id | INT IDENTITY | No | PK |
| Tours.Name | NVARCHAR(200) | No | |
| Tours.Description | NVARCHAR(1000) | Yes | |
| Tours.CreatedByUserId | INT FK | No | |
| Tours.IsPublic | BIT | No | |
| TourStops.TourId | INT FK | No | CASCADE DELETE |
| TourStops.PoiId | INT FK | No | |
| TourStops.OrderIndex | INT | No | UNIQUE (TourId, OrderIndex) |
| TourStops.Notes | NVARCHAR(500) | Yes | |

## 13. AuditLogs
| Cột | Kiểu | NULL | Mô tả |
|---|---|---|---|
| Id | BIGINT IDENTITY | No | PK |
| UserId | INT FK | Yes | null = system |
| Action | NVARCHAR(50) | No | `Create`/`Update`/`Delete`/`Approve`/`Reject`/`Lock`/... |
| EntityType | NVARCHAR(50) | No | `POI`/`Audio`/`Tour`/... |
| EntityId | INT | Yes | |
| BeforeJson / AfterJson | NVARCHAR(MAX) | Yes | snapshot JSON |
| CreatedAt | DATETIME2 | No | |

## Quy ước chung
- Tất cả `CreatedAt`, `UpdatedAt` lưu UTC (`SYSUTCDATETIME()`), frontend tự convert theo múi giờ.
- Mọi bảng đều có soft-delete qua `IsActive` (trừ log tables).
- Index được thiết kế cho query phổ biến: filter theo status, sort theo ngày, tra cứu theo user/device.
- Dùng `NVARCHAR` + collation Unicode để đảm bảo hỗ trợ tiếng Việt.

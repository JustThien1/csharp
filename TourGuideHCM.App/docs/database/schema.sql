-- ============================================================================
-- TourGuideHCM — Database Schema (SQL Server / T-SQL)
-- Phiên bản: 1.0 — sinh từ ERD 31_database_schema_detailed
-- Ghi chú: có thể adapt sang MySQL/Postgres bằng cách đổi:
--   NVARCHAR -> VARCHAR, DATETIME2 -> DATETIME/TIMESTAMP, BIT -> BOOLEAN,
--   IDENTITY  -> AUTO_INCREMENT/SERIAL.
-- ============================================================================

IF DB_ID('TourGuideHCM') IS NULL
    CREATE DATABASE TourGuideHCM;
GO
USE TourGuideHCM;
GO

-- ============================================================================
-- 1. USERS  — người dùng hệ thống (3 role: Admin, Saler, User)
-- ============================================================================
CREATE TABLE dbo.Users (
    Id              INT IDENTITY(1,1) NOT NULL,
    Username        NVARCHAR(100)     NOT NULL,
    PasswordHash    NVARCHAR(255)     NOT NULL,
    Email           NVARCHAR(255)     NOT NULL,
    FullName        NVARCHAR(200)     NULL,
    Role            NVARCHAR(20)      NOT NULL, -- Admin | Saler | User
    IsActive        BIT               NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt       DATETIME2         NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2         NULL,
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Saler', N'User'))
);
GO

-- ============================================================================
-- 2. CATEGORIES — loại POI (di tích, bảo tàng, ẩm thực, ...)
-- ============================================================================
CREATE TABLE dbo.Categories (
    Id          INT IDENTITY(1,1) NOT NULL,
    Name        NVARCHAR(100)     NOT NULL,
    Description NVARCHAR(500)     NULL,
    IconUrl     NVARCHAR(255)     NULL,
    IsActive    BIT               NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
    CreatedAt   DATETIME2         NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Categories PRIMARY KEY (Id),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name)
);
GO

-- ============================================================================
-- 3. POIs — điểm tham quan (Point of Interest)
-- ============================================================================
CREATE TABLE dbo.POIs (
    Id                INT IDENTITY(1,1) NOT NULL,
    Name              NVARCHAR(200)     NOT NULL,
    Address           NVARCHAR(500)     NULL,
    Lat               FLOAT             NOT NULL,
    Lng               FLOAT             NOT NULL,
    Radius            FLOAT             NOT NULL CONSTRAINT DF_POIs_Radius DEFAULT (50), -- mét
    NarrationText     NVARCHAR(MAX)     NULL,
    CategoryId        INT               NOT NULL,
    CreatedByUserId   INT               NULL,  -- NULL khi do Admin seed
    ReviewStatus      NVARCHAR(20)      NOT NULL CONSTRAINT DF_POIs_ReviewStatus DEFAULT (N'Pending'),
    RejectionReason   NVARCHAR(500)     NULL,
    MergedIntoId      INT               NULL,  -- trỏ sang POI giữ lại khi bị merge
    IsActive          BIT               NOT NULL CONSTRAINT DF_POIs_IsActive DEFAULT (1),
    IsLocked          BIT               NOT NULL CONSTRAINT DF_POIs_IsLocked DEFAULT (0),
    CreatedAt         DATETIME2         NOT NULL CONSTRAINT DF_POIs_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt         DATETIME2         NULL,
    CONSTRAINT PK_POIs PRIMARY KEY (Id),
    CONSTRAINT FK_POIs_Category    FOREIGN KEY (CategoryId)      REFERENCES dbo.Categories(Id),
    CONSTRAINT FK_POIs_CreatedBy   FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_POIs_MergedInto  FOREIGN KEY (MergedIntoId)    REFERENCES dbo.POIs(Id),
    CONSTRAINT CK_POIs_ReviewStatus CHECK (ReviewStatus IN (N'Draft', N'Pending', N'Approved', N'Rejected', N'Locked')),
    CONSTRAINT CK_POIs_Lat CHECK (Lat BETWEEN -90  AND 90),
    CONSTRAINT CK_POIs_Lng CHECK (Lng BETWEEN -180 AND 180),
    CONSTRAINT CK_POIs_Radius CHECK (Radius > 0)
);
GO
CREATE INDEX IX_POIs_ReviewStatus      ON dbo.POIs(ReviewStatus) WHERE IsActive = 1;
CREATE INDEX IX_POIs_CreatedByUserId   ON dbo.POIs(CreatedByUserId);
CREATE INDEX IX_POIs_CategoryId        ON dbo.POIs(CategoryId);
CREATE INDEX IX_POIs_LatLng            ON dbo.POIs(Lat, Lng) WHERE IsActive = 1;
GO

-- ============================================================================
-- 4. Audios — file âm thanh thuyết minh (mỗi POI có nhiều ngôn ngữ)
-- ============================================================================
CREATE TABLE dbo.Audios (
    Id              INT IDENTITY(1,1) NOT NULL,
    PoiId           INT               NOT NULL,
    Language        NVARCHAR(10)      NOT NULL, -- vi | en | ...
    AudioUrl        NVARCHAR(500)     NOT NULL,
    FileName        NVARCHAR(255)     NULL,
    DurationSeconds INT               NULL,
    SizeBytes       BIGINT            NULL,
    Source          NVARCHAR(20)      NOT NULL CONSTRAINT DF_Audios_Source DEFAULT (N'Upload'), -- Upload|TTS
    IsActive        BIT               NOT NULL CONSTRAINT DF_Audios_IsActive DEFAULT (1),
    CreatedAt       DATETIME2         NOT NULL CONSTRAINT DF_Audios_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Audios PRIMARY KEY (Id),
    CONSTRAINT FK_Audios_POI FOREIGN KEY (PoiId) REFERENCES dbo.POIs(Id),
    CONSTRAINT UQ_Audios_PoiLang UNIQUE (PoiId, Language, IsActive),
    CONSTRAINT CK_Audios_Source CHECK (Source IN (N'Upload', N'TTS'))
);
GO
CREATE INDEX IX_Audios_PoiId ON dbo.Audios(PoiId);
GO

-- ============================================================================
-- 5. TtsJobs — hàng đợi chuyển text -> speech (bất đồng bộ)
-- ============================================================================
CREATE TABLE dbo.TtsJobs (
    Id                  INT IDENTITY(1,1) NOT NULL,
    PoiId               INT               NOT NULL,
    Language            NVARCHAR(10)      NOT NULL,
    InputText           NVARCHAR(MAX)     NOT NULL,
    Status              NVARCHAR(20)      NOT NULL CONSTRAINT DF_TtsJobs_Status DEFAULT (N'Pending'),
    AudioUrl            NVARCHAR(500)     NULL,
    ErrorMessage        NVARCHAR(1000)    NULL,
    RequestedByUserId   INT               NOT NULL,
    CreatedAt           DATETIME2         NOT NULL CONSTRAINT DF_TtsJobs_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CompletedAt         DATETIME2         NULL,
    CONSTRAINT PK_TtsJobs PRIMARY KEY (Id),
    CONSTRAINT FK_TtsJobs_POI         FOREIGN KEY (PoiId)             REFERENCES dbo.POIs(Id),
    CONSTRAINT FK_TtsJobs_RequestedBy FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_TtsJobs_Status CHECK (Status IN (N'Pending', N'Processing', N'Done', N'Failed'))
);
GO
CREATE INDEX IX_TtsJobs_Status ON dbo.TtsJobs(Status) INCLUDE (PoiId, Language);
GO

-- ============================================================================
-- 6. DuplicateReports — cảnh báo POI trùng (do service tự dò)
-- ============================================================================
CREATE TABLE dbo.DuplicateReports (
    Id                   INT IDENTITY(1,1) NOT NULL,
    PoiAId               INT               NOT NULL,
    PoiBId               INT               NOT NULL,
    Level                NVARCHAR(20)      NOT NULL, -- High|Medium|Low
    Score                FLOAT             NOT NULL, -- 0..1
    Status               NVARCHAR(20)      NOT NULL CONSTRAINT DF_DupReport_Status DEFAULT (N'Open'),
    ResolvedByUserId     INT               NULL,
    ResolutionNote       NVARCHAR(500)     NULL,
    CreatedAt            DATETIME2         NOT NULL CONSTRAINT DF_DupReport_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ResolvedAt           DATETIME2         NULL,
    CONSTRAINT PK_DuplicateReports PRIMARY KEY (Id),
    CONSTRAINT FK_DupReport_PoiA      FOREIGN KEY (PoiAId) REFERENCES dbo.POIs(Id),
    CONSTRAINT FK_DupReport_PoiB      FOREIGN KEY (PoiBId) REFERENCES dbo.POIs(Id),
    CONSTRAINT FK_DupReport_ResolvedBy FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT UQ_DupReport_Pair UNIQUE (PoiAId, PoiBId),
    CONSTRAINT CK_DupReport_Level  CHECK (Level  IN (N'High', N'Medium', N'Low')),
    CONSTRAINT CK_DupReport_Status CHECK (Status IN (N'Open', N'Merged', N'Dismissed')),
    CONSTRAINT CK_DupReport_Score  CHECK (Score BETWEEN 0 AND 1),
    CONSTRAINT CK_DupReport_Distinct CHECK (PoiAId <> PoiBId)
);
GO
CREATE INDEX IX_DupReport_Status ON dbo.DuplicateReports(Status);
GO

-- ============================================================================
-- 7. Notifications — thông báo hệ thống cho từng user
-- ============================================================================
CREATE TABLE dbo.Notifications (
    Id                  INT IDENTITY(1,1) NOT NULL,
    UserId              INT               NOT NULL,
    Type                NVARCHAR(50)      NOT NULL,
    Title               NVARCHAR(200)     NOT NULL,
    Message             NVARCHAR(1000)    NULL,
    RelatedEntityId     INT               NULL,
    RelatedEntityType   NVARCHAR(50)      NULL, -- POI | Audio | TtsJob | DuplicateReport ...
    IsRead              BIT               NOT NULL CONSTRAINT DF_Noti_IsRead DEFAULT (0),
    CreatedAt           DATETIME2         NOT NULL CONSTRAINT DF_Noti_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ReadAt              DATETIME2         NULL,
    CONSTRAINT PK_Notifications PRIMARY KEY (Id),
    CONSTRAINT FK_Noti_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO
CREATE INDEX IX_Noti_UserId_IsRead ON dbo.Notifications(UserId, IsRead) INCLUDE (CreatedAt);
GO

-- ============================================================================
-- 8. PlaybackLogs — nhật ký nghe thuyết minh
-- ============================================================================
CREATE TABLE dbo.PlaybackLogs (
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    UserId          INT               NULL,
    DeviceId        NVARCHAR(100)     NOT NULL,
    PoiId           INT               NOT NULL,
    AudioId         INT               NULL,
    Language        NVARCHAR(10)      NULL,
    DurationSeconds INT               NOT NULL CONSTRAINT DF_Playback_Duration DEFAULT (0),
    Completed       BIT               NOT NULL CONSTRAINT DF_Playback_Completed DEFAULT (0),
    PlayedAt        DATETIME2         NOT NULL CONSTRAINT DF_Playback_PlayedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_PlaybackLogs PRIMARY KEY (Id),
    CONSTRAINT FK_Playback_User  FOREIGN KEY (UserId)  REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Playback_POI   FOREIGN KEY (PoiId)   REFERENCES dbo.POIs(Id),
    CONSTRAINT FK_Playback_Audio FOREIGN KEY (AudioId) REFERENCES dbo.Audios(Id)
);
GO
CREATE INDEX IX_Playback_PlayedAt ON dbo.PlaybackLogs(PlayedAt DESC);
CREATE INDEX IX_Playback_Poi_Date ON dbo.PlaybackLogs(PoiId, PlayedAt DESC);
GO

-- ============================================================================
-- 9. RouteLogs — vệt di chuyển của người dùng (bulk insert)
-- ============================================================================
CREATE TABLE dbo.RouteLogs (
    Id           BIGINT IDENTITY(1,1) NOT NULL,
    UserId       INT               NULL,
    DeviceId     NVARCHAR(100)     NOT NULL,
    Lat          FLOAT             NOT NULL,
    Lng          FLOAT             NOT NULL,
    Accuracy     FLOAT             NULL,
    Speed        FLOAT             NULL,
    RecordedAt   DATETIME2         NOT NULL,
    CONSTRAINT PK_RouteLogs PRIMARY KEY (Id),
    CONSTRAINT FK_Route_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Route_Lat CHECK (Lat BETWEEN -90  AND 90),
    CONSTRAINT CK_Route_Lng CHECK (Lng BETWEEN -180 AND 180)
);
GO
CREATE INDEX IX_Route_RecordedAt ON dbo.RouteLogs(RecordedAt DESC);
CREATE INDEX IX_Route_Device     ON dbo.RouteLogs(DeviceId, RecordedAt DESC);
GO

-- ============================================================================
-- 10. UserHeartbeats — theo dõi online/offline realtime cho admin
-- ============================================================================
CREATE TABLE dbo.UserHeartbeats (
    Id        INT IDENTITY(1,1) NOT NULL,
    UserId    INT               NULL,
    DeviceId  NVARCHAR(100)     NOT NULL,
    LastLat   FLOAT             NULL,
    LastLng   FLOAT             NULL,
    LastSeen  DATETIME2         NOT NULL CONSTRAINT DF_HB_LastSeen DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_UserHeartbeats PRIMARY KEY (Id),
    CONSTRAINT FK_HB_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT UQ_HB_Device UNIQUE (DeviceId)
);
GO
CREATE INDEX IX_HB_LastSeen ON dbo.UserHeartbeats(LastSeen DESC);
GO

-- ============================================================================
-- 11. FavoritePOIs — bookmark của user (App)
-- ============================================================================
CREATE TABLE dbo.FavoritePOIs (
    Id         INT IDENTITY(1,1) NOT NULL,
    UserId     INT               NOT NULL,
    PoiId      INT               NOT NULL,
    CreatedAt  DATETIME2         NOT NULL CONSTRAINT DF_Fav_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_FavoritePOIs PRIMARY KEY (Id),
    CONSTRAINT FK_Fav_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Fav_POI  FOREIGN KEY (PoiId)  REFERENCES dbo.POIs(Id),
    CONSTRAINT UQ_Fav_User_POI UNIQUE (UserId, PoiId)
);
GO

-- ============================================================================
-- 12. Tours & TourStops — chuỗi POI đã sắp xếp thành tour
-- ============================================================================
CREATE TABLE dbo.Tours (
    Id               INT IDENTITY(1,1) NOT NULL,
    Name             NVARCHAR(200)     NOT NULL,
    Description      NVARCHAR(1000)    NULL,
    CreatedByUserId  INT               NOT NULL,
    IsPublic         BIT               NOT NULL CONSTRAINT DF_Tours_IsPublic DEFAULT (0),
    CreatedAt        DATETIME2         NOT NULL CONSTRAINT DF_Tours_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Tours PRIMARY KEY (Id),
    CONSTRAINT FK_Tours_User FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id)
);
GO
CREATE TABLE dbo.TourStops (
    Id         INT IDENTITY(1,1) NOT NULL,
    TourId     INT               NOT NULL,
    PoiId      INT               NOT NULL,
    OrderIndex INT               NOT NULL,
    Notes      NVARCHAR(500)     NULL,
    CONSTRAINT PK_TourStops PRIMARY KEY (Id),
    CONSTRAINT FK_TS_Tour FOREIGN KEY (TourId) REFERENCES dbo.Tours(Id) ON DELETE CASCADE,
    CONSTRAINT FK_TS_POI  FOREIGN KEY (PoiId)  REFERENCES dbo.POIs(Id),
    CONSTRAINT UQ_TS_Order UNIQUE (TourId, OrderIndex)
);
GO

-- ============================================================================
-- 13. AuditLogs — vết thao tác ghi/sửa/xoá (dùng cho moderation)
-- ============================================================================
CREATE TABLE dbo.AuditLogs (
    Id          BIGINT IDENTITY(1,1) NOT NULL,
    UserId      INT               NULL,
    Action      NVARCHAR(50)      NOT NULL, -- Create | Update | Delete | Approve | Reject | Lock | ...
    EntityType  NVARCHAR(50)      NOT NULL, -- POI | Audio | Tour ...
    EntityId    INT               NULL,
    BeforeJson  NVARCHAR(MAX)     NULL,
    AfterJson   NVARCHAR(MAX)     NULL,
    CreatedAt   DATETIME2         NOT NULL CONSTRAINT DF_Audit_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_AuditLogs PRIMARY KEY (Id),
    CONSTRAINT FK_Audit_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO
CREATE INDEX IX_Audit_Entity ON dbo.AuditLogs(EntityType, EntityId, CreatedAt DESC);
GO

-- ============================================================================
-- Seed tối thiểu (admin mặc định + vài category)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
    INSERT INTO dbo.Users (Username, PasswordHash, Email, FullName, Role, IsActive)
    VALUES (N'admin', N'<BCRYPT_HASH_PLACEHOLDER>', N'admin@tourguidehcm.local', N'System Admin', N'Admin', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
    INSERT INTO dbo.Categories (Name, Description) VALUES
        (N'Di tích lịch sử', N'Lăng tẩm, đình chùa, bảo tàng'),
        (N'Bảo tàng',        N'Bảo tàng nhà nước và tư nhân'),
        (N'Ẩm thực',         N'Đặc sản, quán ăn nổi tiếng'),
        (N'Công viên',       N'Không gian xanh và giải trí'),
        (N'Chợ - Phố đi bộ', N'Địa điểm mua sắm, phố đi bộ');
GO

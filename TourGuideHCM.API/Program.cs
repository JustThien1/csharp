using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Filters;
using TourGuideHCM.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ====================== Database ======================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ====================== Services ======================
builder.Services.AddHttpContextAccessor();   // cần cho CurrentUserService
builder.Services.AddScoped<POIService>();
builder.Services.AddScoped<GeofenceService>();
builder.Services.AddScoped<FavoriteService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<DuplicateDetectionService>();
builder.Services.AddScoped<TtsQueueService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<SalerSubscriptionGuardFilter>();

// ====================== Upload File lớn ======================
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 50 * 1024 * 1024);
builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = 50 * 1024 * 1024);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 50 * 1024 * 1024);

// ====================== JWT ======================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                builder.Configuration["Jwt:Key"] ?? "your-super-secret-key-here-at-least-32-characters-long")),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ====================== Controllers & Swagger ======================
builder.Services.AddControllers(options =>
    {
        options.Filters.AddService<SalerSubscriptionGuardFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TourGuideHCM API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập JWT Token: Bearer {token}",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// ====================== CORS ======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// ====================== Middleware ======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TourGuideHCM API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// ====================== Endpoints ======================
app.MapControllers();

// ====================== Seed Database ======================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();

        // ====================== AUTO-ADD COLUMNS cho Monitoring (SQLite) ======================
        // Thêm cột mới vào PlaybackLogs mà không cần tạo migration
        var columnsToAdd = new[]
        {
            ("DeviceId",    "TEXT"),
            ("DeviceName",  "TEXT"),
            ("Platform",    "TEXT"),
            ("IpAddress",   "TEXT"),
            ("UserName",    "TEXT")
        };
        foreach (var (col, type) in columnsToAdd)
        {
            try
            {
                context.Database.ExecuteSqlRaw($"ALTER TABLE PlaybackLogs ADD COLUMN {col} {type};");
                Console.WriteLine($"   ➕ Đã thêm cột PlaybackLogs.{col}");
            }
            catch { /* đã tồn tại → bỏ qua */ }
        }

        // ====================== AUTO-ADD cột ReviewStatus cho POIs ======================
        try
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE POIs ADD COLUMN ReviewStatus TEXT NOT NULL DEFAULT 'Approved';");
            Console.WriteLine("   ➕ Đã thêm cột POIs.ReviewStatus");
        }
        catch { /* đã tồn tại */ }

        // ====================== FIX FK: PlaybackLogs.POIId NULL cho heartbeat ======================
        // Trước đây heartbeat dùng POIId=0 gây FK constraint failed.
        // Giờ chuyển POIId sang nullable: các heartbeat log hiện có (POIId=0) sẽ được
        // update thành NULL. Đồng thời relax FK check cho các hàng cũ với POIId=0.
        try
        {
            // Bước 1: update các log heartbeat cũ có POIId=0 → NULL
            // (chỉ chạy được nếu cột đã allow NULL. Nếu đang NOT NULL thì skip)
            var updated = context.Database.ExecuteSqlRaw(@"
                UPDATE PlaybackLogs SET POIId = NULL
                WHERE POIId = 0 AND (TriggerType = 'heartbeat' OR TriggerType = 'online');
            ");
            if (updated > 0)
                Console.WriteLine($"   🔧 Đã update {updated} heartbeat log: POIId 0 → NULL");
        }
        catch (Exception e)
        {
            // Nếu cột đang NOT NULL, UPDATE sẽ fail. Phải rebuild table.
            Console.WriteLine($"   ⚠ Không update được POIId=0 → NULL: {e.Message}");
            Console.WriteLine($"   ⚠ Có thể cần xoá file tourguide.db để EF tạo lại với schema mới.");
        }

        // ====================== AUTO-CREATE bảng DuplicateReports ======================
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS DuplicateReports (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PoiAId INTEGER NOT NULL,
                    PoiBId INTEGER NOT NULL,
                    NameSimilarity REAL NOT NULL DEFAULT 0,
                    DistanceMeters REAL NOT NULL DEFAULT 0,
                    Level TEXT NOT NULL DEFAULT 'Medium',
                    Status TEXT NOT NULL DEFAULT 'Open',
                    Resolution TEXT NULL,
                    ResolutionNote TEXT NULL,
                    CreatedAt TEXT NOT NULL,
                    ResolvedAt TEXT NULL,
                    ResolvedBy TEXT NULL,
                    IsDismissed INTEGER NOT NULL DEFAULT 0,
                    DismissedAt TEXT NULL,
                    FOREIGN KEY (PoiAId) REFERENCES POIs(Id),
                    FOREIGN KEY (PoiBId) REFERENCES POIs(Id)
                );
                CREATE INDEX IF NOT EXISTS IX_DuplicateReports_Status ON DuplicateReports(Status);
            ");
            Console.WriteLine("   ➕ Đã tạo bảng DuplicateReports");
        }
        catch (Exception e) { Console.WriteLine($"   ⚠ DuplicateReports: {e.Message}"); }

        // ====================== AUTO-CREATE bảng TtsJobs ======================
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS TtsJobs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PoiId INTEGER NOT NULL,
                    Language TEXT NOT NULL DEFAULT 'vi',
                    Gender TEXT NOT NULL DEFAULT 'female',
                    Text TEXT NOT NULL,
                    Speed REAL NOT NULL DEFAULT 1.0,
                    Status TEXT NOT NULL DEFAULT 'Pending',
                    AudioUrl TEXT NULL,
                    ErrorMessage TEXT NULL,
                    RetryCount INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    StartedAt TEXT NULL,
                    CompletedAt TEXT NULL,
                    FOREIGN KEY (PoiId) REFERENCES POIs(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_TtsJobs_Status ON TtsJobs(Status);
            ");
            Console.WriteLine("   ➕ Đã tạo bảng TtsJobs");
        }
        catch (Exception e) { Console.WriteLine($"   ⚠ TtsJobs: {e.Message}"); }

        // ====================== AUTO-ADD cột cho Users (Role, LastLoginAt) ======================
        var userColumns = new[]
        {
            ("Role", "TEXT NOT NULL DEFAULT 'Saler'"),
            ("LastLoginAt", "TEXT NULL")
        };
        foreach (var (col, def) in userColumns)
        {
            try
            {
                context.Database.ExecuteSqlRaw($"ALTER TABLE Users ADD COLUMN {col} {def};");
                Console.WriteLine($"   ➕ Đã thêm cột Users.{col}");
            }
            catch { /* đã tồn tại */ }
        }

        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN SubscriptionEndUtc TEXT NULL;");
            Console.WriteLine("   ➕ Đã thêm cột Users.SubscriptionEndUtc");
        }
        catch { /* đã tồn tại */ }

        try
        {
            var n = context.Database.ExecuteSqlRaw(@"
                UPDATE Users SET SubscriptionEndUtc = datetime('now', '+3650 days')
                WHERE Role = 'Saler' AND SubscriptionEndUtc IS NULL;");
            if (n > 0)
                Console.WriteLine($"   🔧 Đã gán SubscriptionEndUtc cho {n} Saler (tài khoản cũ chưa có ngày hết hạn).");
        }
        catch (Exception e) { Console.WriteLine($"   ⚠ SubscriptionEndUtc backfill: {e.Message}"); }

        // ====================== AUTO-ADD cột cho POIs (Saler feature) ======================
        var poiColumns = new[]
        {
            ("RejectionReason", "TEXT NULL"),
            ("CreatedByUserId", "INTEGER NULL"),
            ("ReviewedAt", "TEXT NULL"),
            ("ReviewedByUserId", "INTEGER NULL")
        };
        foreach (var (col, def) in poiColumns)
        {
            try
            {
                context.Database.ExecuteSqlRaw($"ALTER TABLE POIs ADD COLUMN {col} {def};");
                Console.WriteLine($"   ➕ Đã thêm cột POIs.{col}");
            }
            catch { /* đã tồn tại */ }
        }

        // POIs.CreatedAt: SQLite không cho ADD COLUMN NOT NULL với DEFAULT non-constant.
        // Phải làm 2 bước: ADD nullable → UPDATE các dòng NULL về now.
        try
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE POIs ADD COLUMN CreatedAt TEXT NULL;");
            Console.WriteLine("   ➕ Đã thêm cột POIs.CreatedAt");
        }
        catch { /* đã tồn tại */ }

        // Fill giá trị cho các POI cũ chưa có CreatedAt
        try
        {
            var updated = context.Database.ExecuteSqlRaw(
                "UPDATE POIs SET CreatedAt = datetime('now') WHERE CreatedAt IS NULL;");
            if (updated > 0)
                Console.WriteLine($"   🔧 Đã set CreatedAt cho {updated} POI cũ");
        }
        catch { }

        // ====================== AUTO-CREATE bảng Notifications ======================
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Notifications (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    Type TEXT NOT NULL DEFAULT 'System',
                    Title TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    RelatedPoiId INTEGER NULL,
                    IsRead INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    ReadAt TEXT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS IX_Notifications_UserId_IsRead
                    ON Notifications(UserId, IsRead);
            ");
            Console.WriteLine("   ➕ Đã tạo bảng Notifications");
        }
        catch (Exception e) { Console.WriteLine($"   ⚠ Notifications: {e.Message}"); }

        // ====================== SEED ADMIN MẶC ĐỊNH ======================
        // Nếu chưa có user Admin nào → tạo tài khoản admin/admin123
        var hasAdmin = context.Users.Any(u => u.Role == "Admin");
        if (!hasAdmin)
        {
            var adminUser = new TourGuideHCM.API.Models.User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                FullName = "Quản trị viên",
                Email = "admin@tourguide-hcm.local",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser);
            context.SaveChanges();
            Console.WriteLine("   👑 Đã tạo tài khoản Admin mặc định: admin / admin123");
            Console.WriteLine("   ⚠  Nhớ đổi mật khẩu sau khi đăng nhập!");
        }

        // ====================== AUTO-SEED CATEGORIES MỚI ======================
        // Thêm các category mới nếu chưa có (dùng tên làm key unique)
        var desiredCategories = new[]
        {
            "Di tích lịch sử",
            "Ẩm thực",
            "Mua sắm",
            "Cafe",
            "Quán ăn",
            "Nhà hàng",
            "Khách sạn",
            "Công viên"
        };

        foreach (var name in desiredCategories)
        {
            if (!context.Categories.Any(c => c.Name == name))
            {
                context.Categories.Add(new TourGuideHCM.API.Models.Category { Name = name });
                Console.WriteLine($"   🏷  Đã thêm danh mục: {name}");
            }
        }
        context.SaveChanges();

        DbSeeder.Seed(context);

        Console.WriteLine("✅ Database SQLite đã sẵn sàng!");
        Console.WriteLine($"   POI: {context.POIs.Count()}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database Error: {ex.Message}");
    }
}

app.Run("http://0.0.0.0:8080");
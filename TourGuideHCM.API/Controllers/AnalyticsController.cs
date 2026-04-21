using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AnalyticsController(AppDbContext context)
    {
        _context = context;
    }

    // ====================== ENDPOINT: App gửi log khi nghe audio ======================
    [HttpPost("playback")]
    public async Task<IActionResult> LogPlayback([FromBody] PlaybackLogRequest request)
    {
        try
        {
            var ip = GetClientIp();
            var userName = await ResolveUserNameAsync(request.UserId, request.DeviceId);

            var log = new PlaybackLog
            {
                UserId = request.UserId > 0 ? request.UserId : null,
                POIId = request.POIId,
                TriggerType = string.IsNullOrWhiteSpace(request.TriggerType) ? "manual" : request.TriggerType,
                TriggeredAt = DateTime.UtcNow,
                DurationSeconds = request.DurationSeconds,
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                Platform = request.Platform,
                IpAddress = ip,
                UserName = userName
            };

            _context.PlaybackLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = log.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LogPlayback Error] {ex.Message}");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ====================== ENDPOINT MỚI: Heartbeat (app ping mỗi 30s) ======================
    // Cho phép user mở app mà chưa nghe audio vẫn được tính "online"
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { message = "DeviceId bắt buộc" });

        try
        {
            var ip = GetClientIp();
            var userName = await ResolveUserNameAsync(request.UserId, request.DeviceId);

            var log = new PlaybackLog
            {
                UserId = request.UserId > 0 ? request.UserId : null,
                POIId = null,                           // null = không liên quan POI (heartbeat)
                TriggerType = "heartbeat",
                TriggeredAt = DateTime.UtcNow,
                DurationSeconds = 0,
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                Platform = request.Platform,
                IpAddress = ip,
                UserName = userName
            };

            _context.PlaybackLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, serverTime = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Heartbeat Error] {ex.Message}");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ====================== DASHBOARD REALTIME ======================
    [HttpGet("dashboard")]
    [HttpGet("realtime")]
    public async Task<ActionResult<DashboardDto>> GetRealtimeDashboard()
    {
        var now = DateTime.UtcNow;

        // ====================== NGƯỠNG REALTIME ======================
        // App gửi heartbeat mỗi 10s → ngưỡng offline 30s (= 3× heartbeat, chịu được 2 lần fail).
        // Ngưỡng listening 15s (tương đương 1 chu kỳ heartbeat + chút buffer).
        var offlineThreshold = now.AddSeconds(-30);     // trước đây: -2 phút
        var listeningThreshold = now.AddSeconds(-15);   // trước đây: -1 phút
        var startOfDay = now.Date;

        try
        {
            // --- Số THIẾT BỊ (unique DeviceId) online trong 30s qua ---
            var onlineDevices = await _context.PlaybackLogs
                .Where(pl => pl.TriggeredAt >= offlineThreshold && pl.DeviceId != null && pl.DeviceId != "")
                .Select(pl => pl.DeviceId)
                .Distinct()
                .CountAsync();

            // --- Số USER (unique UserId > 0) online trong 30s qua ---
            var onlineUsers = await _context.PlaybackLogs
                .Where(pl => pl.TriggeredAt >= offlineThreshold && pl.UserId != null && pl.UserId > 0)
                .Select(pl => pl.UserId)
                .Distinct()
                .CountAsync();

            // Nếu không có user đăng nhập nhưng có thiết bị → hiển thị bằng số thiết bị
            if (onlineUsers == 0 && onlineDevices > 0)
                onlineUsers = onlineDevices;

            // --- Số người đang NGHE audio (có POIId > 0, triggerType khác heartbeat, trong 15s qua) ---
            var listeningUsers = await _context.PlaybackLogs
                .Where(pl => pl.TriggeredAt >= listeningThreshold
                          && pl.POIId > 0
                          && pl.TriggerType != "heartbeat"
                          && pl.TriggerType != "online"
                          && pl.DeviceId != null)
                .Select(pl => pl.DeviceId)
                .Distinct()
                .CountAsync();

            // --- POI nóng nhất (24h qua) ---
            var hotPoiGroup = await _context.PlaybackLogs
                .Where(pl => pl.TriggeredAt >= now.AddHours(-24) && pl.POIId > 0
                          && pl.TriggerType != "heartbeat" && pl.TriggerType != "online")
                .GroupBy(pl => pl.POIId)
                .Select(g => new { POIId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            string hotPoiName = "Chưa có dữ liệu";
            if (hotPoiGroup != null)
            {
                var poi = await _context.POIs.FindAsync(hotPoiGroup.POIId);
                hotPoiName = poi?.Name ?? $"POI #{hotPoiGroup.POIId}";
            }

            // --- Thời gian nghe trung bình ---
            var avgListenSeconds = await _context.PlaybackLogs
                .Where(pl => pl.DurationSeconds != null && pl.DurationSeconds > 0)
                .AverageAsync(pl => pl.DurationSeconds) ?? 0;

            // --- Tổng lượt nghe hôm nay ---
            var totalToday = await _context.PlaybackLogs
                .Where(pl => pl.TriggeredAt >= startOfDay
                          && pl.POIId > 0
                          && pl.TriggerType != "heartbeat" && pl.TriggerType != "online")
                .CountAsync();

            // --- Danh sách session đang active (group theo DeviceId, lấy log mới nhất) ---
            var recentLogs = await _context.PlaybackLogs
                .Where(pl => pl.TriggeredAt >= offlineThreshold && pl.DeviceId != null && pl.DeviceId != "")
                .Include(pl => pl.POI)
                .OrderByDescending(pl => pl.TriggeredAt)
                .Take(200)
                .ToListAsync();

            var activeSessions = recentLogs
                .GroupBy(pl => pl.DeviceId!)
                .Select(g =>
                {
                    var latest = g.First();   // đã OrderByDescending
                    var latestListening = g.FirstOrDefault(x => x.POIId > 0
                        && x.TriggerType != "heartbeat" && x.TriggerType != "online");
                    var status = "idle";
                    // Listening: có log POI trong 15s qua
                    if (latestListening != null && (now - latestListening.TriggeredAt).TotalSeconds < 15)
                        status = "listening";
                    // Online: có heartbeat trong 30s qua
                    else if ((now - latest.TriggeredAt).TotalSeconds < 30)
                        status = "online";

                    return new ActiveSession
                    {
                        UserName = latest.UserName ?? $"Khách_{ShortDevice(latest.DeviceId)}",
                        CurrentPoi = latestListening?.POI?.Name ?? "—",
                        ConnectedTime = GetRelativeTime(latest.TriggeredAt),
                        DeviceId = ShortDevice(latest.DeviceId),
                        DeviceName = latest.DeviceName ?? "Không xác định",
                        Platform = latest.Platform ?? "?",
                        IpAddress = latest.IpAddress ?? "-",
                        Status = status
                    };
                })
                .OrderByDescending(s => s.Status == "listening")
                .ThenByDescending(s => s.Status == "online")
                .Take(20)
                .ToList();

            // --- Phân tích theo platform ---
            var deviceBreakdown = recentLogs
                .Where(x => !string.IsNullOrEmpty(x.DeviceId))
                .GroupBy(x => x.DeviceId!)
                .Select(g => g.First().Platform ?? "Unknown")
                .GroupBy(p => p)
                .Select(g => new DeviceBreakdown { Platform = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var dto = new DashboardDto
            {
                OnlineUsers = onlineUsers,
                OnlineDevices = onlineDevices,
                ListeningUsers = listeningUsers,
                HotPoiName = hotPoiName,
                AvgListenTime = (int)Math.Round(avgListenSeconds),
                TotalToday = totalToday,
                ActiveSessions = activeSessions,
                DeviceBreakdown = deviceBreakdown
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Analytics Error] {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ====================== ENDPOINT MỚI: Danh sách thiết bị chi tiết ======================
    [HttpGet("devices")]
    public async Task<IActionResult> GetActiveDevices()
    {
        var now = DateTime.UtcNow;
        var offlineThreshold = now.AddSeconds(-30);   // 30s: cùng ngưỡng với dashboard realtime

        var recentLogs = await _context.PlaybackLogs
            .Where(pl => pl.TriggeredAt >= offlineThreshold && pl.DeviceId != null && pl.DeviceId != "")
            .Include(pl => pl.POI)
            .OrderByDescending(pl => pl.TriggeredAt)
            .Take(500)
            .ToListAsync();

        var devices = recentLogs
            .GroupBy(pl => pl.DeviceId!)
            .Select(g =>
            {
                var latest = g.First();
                return new
                {
                    deviceId = latest.DeviceId,
                    deviceName = latest.DeviceName,
                    platform = latest.Platform,
                    ipAddress = latest.IpAddress,
                    userName = latest.UserName,
                    lastSeen = latest.TriggeredAt,
                    lastPoi = latest.POI?.Name,
                    secondsAgo = (int)(now - latest.TriggeredAt).TotalSeconds
                };
            })
            .OrderBy(x => x.secondsAgo)
            .ToList();

        return Ok(devices);
    }

    // ====================== ENDPOINT: Dữ liệu mẫu để test UI khi không có app chạy ======================
    [HttpPost("seed-demo")]
    public async Task<IActionResult> SeedDemo()
    {
        var rand = new Random();
        var now = DateTime.UtcNow;
        var platforms = new[] { "Android", "iOS", "Windows" };
        var names = new[] { "Pixel 7", "iPhone 14 Pro", "Samsung SM-A515", "Xiaomi 13", "iPhone SE" };

        var pois = await _context.POIs.Take(5).ToListAsync();
        if (pois.Count == 0)
            return BadRequest(new { message = "Cần có POI trong database trước" });

        for (int i = 0; i < 8; i++)
        {
            var devId = $"demo-{Guid.NewGuid().ToString()[..8]}";
            var platform = platforms[rand.Next(platforms.Length)];
            var name = names[rand.Next(names.Length)];
            var poi = pois[rand.Next(pois.Count)];

            // heartbeat mới nhất (vừa xong)
            _context.PlaybackLogs.Add(new PlaybackLog
            {
                POIId = null,                     // null = heartbeat (không gắn POI)
                TriggerType = "heartbeat",
                TriggeredAt = now.AddSeconds(-rand.Next(0, 90)),
                DeviceId = devId,
                DeviceName = name,
                Platform = platform,
                IpAddress = $"192.168.1.{rand.Next(2, 250)}",
                UserName = $"Khách_demo{i + 1}"
            });

            // Nghe POI (50% chance)
            if (rand.Next(2) == 0)
            {
                _context.PlaybackLogs.Add(new PlaybackLog
                {
                    POIId = poi.Id,
                    TriggerType = rand.Next(2) == 0 ? "geofence" : "manual",
                    TriggeredAt = now.AddSeconds(-rand.Next(0, 50)),
                    DurationSeconds = rand.Next(30, 180),
                    DeviceId = devId,
                    DeviceName = name,
                    Platform = platform,
                    IpAddress = $"192.168.1.{rand.Next(2, 250)}",
                    UserName = $"Khách_demo{i + 1}"
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã tạo dữ liệu demo. Vào trang Monitoring xem kết quả." });
    }

    // ====================== DASHBOARD ANALYTICS (tổng quan, không realtime) ======================
    /// <summary>
    /// Tổng hợp cho trang Dashboard: tổng POI, tổng thiết bị từng truy cập,
    /// POI nóng nhất, thời gian nghe TB, top POI, và lượt nghe theo ngày trong tuần.
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-6);   // 7 ngày gần nhất

        try
        {
            // Tổng POI đang hoạt động
            var totalPoi = await _context.POIs.CountAsync(p => p.IsActive);

            // Chỉ tính log "nghe thật" — bỏ heartbeat/online
            var playQuery = _context.PlaybackLogs
                .Where(l => l.POIId > 0
                         && l.TriggerType != "heartbeat"
                         && l.TriggerType != "online");

            // Tổng thiết bị khác nhau từng nghe audio
            var totalDevices = await playQuery
                .Where(l => l.DeviceId != null && l.DeviceId != "")
                .Select(l => l.DeviceId)
                .Distinct()
                .CountAsync();

            // Thời gian nghe trung bình (phút)
            var avgSeconds = await playQuery
                .Where(l => l.DurationSeconds != null && l.DurationSeconds > 0)
                .AverageAsync(l => l.DurationSeconds) ?? 0;
            var avgMinutes = (int)Math.Round(avgSeconds / 60.0);

            // Top POI
            var topPoiList = await playQuery
                .Include(l => l.POI)
                .Where(l => l.POI != null)
                .GroupBy(l => new { l.POIId, l.POI!.Name })
                .Select(g => new TopPoiItem
                {
                    Name = g.Key.Name ?? $"POI #{g.Key.POIId}",
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var topPoiName = topPoiList.FirstOrDefault()?.Name ?? "Chưa có dữ liệu";

            // Lượt nghe theo ngày trong tuần (T2..CN)
            var weeklyLogs = await playQuery
                .Where(l => l.TriggeredAt >= weekStart)
                .Select(l => l.TriggeredAt)
                .ToListAsync();

            // DailyViews[0] = T2, [1] = T3, ... [6] = CN
            var dailyViews = new int[7];
            foreach (var t in weeklyLogs)
            {
                var dow = (int)t.DayOfWeek;   // Sunday=0, Monday=1,...Saturday=6
                var idx = dow == 0 ? 6 : dow - 1;   // convert to Mon=0..Sun=6
                dailyViews[idx]++;
            }

            // Phân bố platform (những thiết bị đã từng nghe)
            var platformBreakdown = await playQuery
                .Where(l => l.DeviceId != null && l.Platform != null)
                .GroupBy(l => l.DeviceId!)
                .Select(g => g.OrderByDescending(x => x.TriggeredAt).First().Platform!)
                .ToListAsync();

            var platforms = platformBreakdown
                .GroupBy(p => p)
                .Select(g => new { Platform = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            return Ok(new
            {
                totalPoi,
                totalUsers = totalDevices,   // field cũ giữ để tương thích admin cũ
                totalDevices,
                topPoi = topPoiName,
                avgTime = avgMinutes,
                topPois = topPoiList,
                dailyViews,
                platformBreakdown = platforms
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Overview Error] {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo dữ liệu demo cho Dashboard: rải ~150 lượt nghe trải đều 7 ngày qua
    /// với nhiều thiết bị Android/iOS/Windows khác nhau.
    /// </summary>
    [HttpPost("seed-dashboard-demo")]
    public async Task<IActionResult> SeedDashboardDemo()
    {
        var pois = await _context.POIs.Where(p => p.IsActive).Take(8).ToListAsync();
        if (pois.Count == 0)
            return BadRequest(new { message = "Cần có POI trong database trước" });

        var rand = new Random();
        var now = DateTime.UtcNow;
        var platforms = new[] { "Android", "iOS", "Windows" };
        var deviceNames = new Dictionary<string, string[]>
        {
            ["Android"] = new[] { "Pixel 7", "Pixel 8", "Samsung S23", "Samsung A54", "Xiaomi 13", "Oppo Reno8" },
            ["iOS"] = new[] { "iPhone 14 Pro", "iPhone 15", "iPhone 13", "iPhone SE" },
            ["Windows"] = new[] { "Surface Pro 9", "Dell XPS 13", "ThinkPad X1" }
        };

        // Tạo 12 thiết bị giả
        var devices = new List<(string id, string name, string platform)>();
        for (int i = 0; i < 12; i++)
        {
            var p = platforms[rand.Next(platforms.Length)];
            var nameList = deviceNames[p];
            devices.Add((
                $"demo-dash-{Guid.NewGuid().ToString()[..8]}",
                nameList[rand.Next(nameList.Length)],
                p
            ));
        }

        // Rải ~150 lượt nghe trong 7 ngày qua
        for (int i = 0; i < 150; i++)
        {
            var dev = devices[rand.Next(devices.Count)];
            var poi = pois[rand.Next(pois.Count)];

            // Random time trong 7 ngày, thiên về giờ làm việc (9h-17h)
            var dayOffset = rand.Next(0, 7);
            var hour = 8 + rand.Next(0, 11);   // 8h-18h
            var minute = rand.Next(0, 60);
            var triggeredAt = now.Date.AddDays(-dayOffset).AddHours(hour).AddMinutes(minute);

            _context.PlaybackLogs.Add(new PlaybackLog
            {
                POIId = poi.Id,
                TriggerType = rand.Next(3) switch { 0 => "geofence", 1 => "qr", _ => "manual" },
                TriggeredAt = triggeredAt,
                DurationSeconds = rand.Next(60, 300),
                DeviceId = dev.id,
                DeviceName = dev.name,
                Platform = dev.platform,
                IpAddress = $"192.168.1.{rand.Next(2, 250)}",
                UserName = $"Khách_{dev.id[..8]}"
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Đã tạo 150 lượt nghe giả từ {devices.Count} thiết bị trong 7 ngày qua" });
    }

    // ====================== HELPERS ======================
    private string? GetClientIp()
    {
        try
        {
            var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        catch { return null; }
    }

    private async Task<string> ResolveUserNameAsync(int userId, string? deviceId)
    {
        if (userId > 0)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
                return user.FullName ?? user.Username ?? $"User #{userId}";
        }
        return $"Khách_{ShortDevice(deviceId)}";
    }

    private static string ShortDevice(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return "????";
        return deviceId.Length >= 8 ? deviceId[..8] : deviceId;
    }

    private static string GetRelativeTime(DateTime time)
    {
        var diff = DateTime.UtcNow - time;
        if (diff.TotalSeconds < 10) return "Vừa xong";
        if (diff.TotalMinutes < 1) return $"{(int)diff.TotalSeconds} giây trước";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
        return $"{(int)diff.TotalHours} giờ trước";
    }
}

// ====================== DTO nhận từ app ======================
public class PlaybackLogRequest
{
    public int UserId { get; set; }
    public int POIId { get; set; }
    public string? TriggerType { get; set; }
    public int? DurationSeconds { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
}

public class HeartbeatRequest
{
    public int UserId { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
}

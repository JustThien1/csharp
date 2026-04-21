using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/route")]
    public class RouteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RouteController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>App gọi để lưu vị trí người dùng (hỗ trợ DeviceId cho app không đăng nhập)</summary>
        [HttpPost("log")]
        public async Task<IActionResult> LogLocation([FromBody] RouteLogRequest request)
        {
            // App ẩn danh: có thể không có UserId, chỉ cần DeviceId
            if (string.IsNullOrWhiteSpace(request.DeviceId) && (request.UserId == null || request.UserId <= 0))
                return BadRequest(new { message = "Cần có DeviceId hoặc UserId" });

            var log = new RouteLog
            {
                UserId = (request.UserId != null && request.UserId > 0) ? request.UserId : null,
                Lat = request.Lat,
                Lng = request.Lng,
                DeviceId = request.DeviceId,
                Timestamp = DateTime.UtcNow
            };

            _context.RouteLogs.Add(log);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        /// <summary>
        /// MỚI: Danh sách các thiết bị có tuyến đường. Mỗi thiết bị kèm tên, platform, số điểm, thời gian hoạt động.
        /// Kết hợp RouteLog (tọa độ) + PlaybackLog (tên thiết bị, platform) qua DeviceId.
        /// </summary>
        [HttpGet("devices")]
        public async Task<IActionResult> GetDevicesWithRoutes([FromQuery] int days = 30)
        {
            var from = days > 0 ? DateTime.UtcNow.AddDays(-days) : DateTime.MinValue;

            // Lấy DeviceId đã từng log vị trí
            var routesByDevice = await _context.RouteLogs
                .Where(r => r.DeviceId != null && r.DeviceId != "" && r.Timestamp >= from)
                .GroupBy(r => r.DeviceId!)
                .Select(g => new
                {
                    DeviceId = g.Key,
                    PointCount = g.Count(),
                    FirstSeen = g.Min(x => x.Timestamp),
                    LastSeen = g.Max(x => x.Timestamp)
                })
                .ToListAsync();

            if (routesByDevice.Count == 0)
                return Ok(new List<object>());

            // Lấy thông tin thiết bị (tên, platform) từ PlaybackLog — bản ghi mới nhất
            var deviceIds = routesByDevice.Select(x => x.DeviceId).ToList();
            var deviceInfoMap = await _context.PlaybackLogs
                .Where(p => p.DeviceId != null && deviceIds.Contains(p.DeviceId))
                .GroupBy(p => p.DeviceId!)
                .Select(g => new
                {
                    DeviceId = g.Key,
                    DeviceName = g.OrderByDescending(x => x.TriggeredAt).Select(x => x.DeviceName).FirstOrDefault(),
                    Platform = g.OrderByDescending(x => x.TriggeredAt).Select(x => x.Platform).FirstOrDefault(),
                    UserName = g.OrderByDescending(x => x.TriggeredAt).Select(x => x.UserName).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.DeviceId);

            var result = routesByDevice
                .Select(r =>
                {
                    deviceInfoMap.TryGetValue(r.DeviceId, out var info);
                    return new
                    {
                        deviceId = r.DeviceId,
                        deviceIdShort = r.DeviceId.Length >= 8 ? r.DeviceId[..8] : r.DeviceId,
                        deviceName = info?.DeviceName ?? "Thiết bị ẩn danh",
                        platform = info?.Platform ?? "?",
                        userName = info?.UserName ?? $"Khách_{(r.DeviceId.Length >= 8 ? r.DeviceId[..8] : r.DeviceId)}",
                        pointCount = r.PointCount,
                        firstSeen = r.FirstSeen,
                        lastSeen = r.LastSeen
                    };
                })
                .OrderByDescending(x => x.lastSeen)
                .ToList();

            return Ok(result);
        }

        /// <summary>
        /// MỚI: Lấy tuyến đường của 1 thiết bị theo DeviceId (không cần đăng nhập).
        /// </summary>
        [HttpGet("by-device")]
        public async Task<IActionResult> GetByDevice([FromQuery] string deviceId, [FromQuery] int days = 0)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return BadRequest(new { message = "Vui lòng nhập DeviceId" });

            var query = _context.RouteLogs
                .Where(r => r.DeviceId == deviceId)
                .AsQueryable();

            if (days > 0)
            {
                var from = DateTime.UtcNow.AddDays(-days);
                query = query.Where(r => r.Timestamp >= from);
            }

            var logs = await query
                .OrderBy(r => r.Timestamp)
                .Select(r => new RoutePointDto
                {
                    Lat = r.Lat,
                    Lng = r.Lng,
                    Timestamp = r.Timestamp
                })
                .ToListAsync();

            if (logs.Count == 0)
                return NotFound(new { message = "Thiết bị này chưa có dữ liệu tuyến đường" });

            // Tính tổng quãng đường (km)
            double totalKm = 0;
            for (int i = 1; i < logs.Count; i++)
                totalKm += Haversine(logs[i - 1].Lat, logs[i - 1].Lng, logs[i].Lat, logs[i].Lng);

            // Lấy tên thiết bị từ PlaybackLog gần nhất
            var deviceInfo = await _context.PlaybackLogs
                .Where(p => p.DeviceId == deviceId)
                .OrderByDescending(p => p.TriggeredAt)
                .Select(p => new { p.DeviceName, p.Platform, p.UserName })
                .FirstOrDefaultAsync();

            return Ok(new RouteDeviceDto
            {
                DeviceId = deviceId,
                DeviceIdShort = deviceId.Length >= 8 ? deviceId[..8] : deviceId,
                DeviceName = deviceInfo?.DeviceName ?? "Thiết bị ẩn danh",
                Platform = deviceInfo?.Platform ?? "?",
                UserName = deviceInfo?.UserName ?? $"Khách_{(deviceId.Length >= 8 ? deviceId[..8] : deviceId)}",
                Points = logs,
                TotalKm = Math.Round(totalKm, 2)
            });
        }

        /// <summary>[DEPRECATED] Lấy tuyến theo SĐT — giữ để tương thích ngược</summary>
        [HttpGet("user")]
        public async Task<IActionResult> GetByPhone([FromQuery] string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest("Vui lòng nhập số điện thoại");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == phone.Trim());

            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng với số ĐT này" });

            var logs = await _context.RouteLogs
                .Where(r => r.UserId == user.Id)
                .OrderBy(r => r.Timestamp)
                .Select(r => new RoutePointDto
                {
                    Lat = r.Lat,
                    Lng = r.Lng,
                    Timestamp = r.Timestamp
                })
                .ToListAsync();

            double totalKm = 0;
            for (int i = 1; i < logs.Count; i++)
                totalKm += Haversine(logs[i - 1].Lat, logs[i - 1].Lng, logs[i].Lat, logs[i].Lng);

            return Ok(new RouteUserDto
            {
                UserId = user.Id,
                FullName = user.FullName ?? user.Username,
                Phone = user.Phone ?? "",
                Points = logs,
                TotalKm = Math.Round(totalKm, 2)
            });
        }

        /// <summary>Admin: tổng hợp tất cả tuyến đường (ẩn danh, dùng cho heatmap)</summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] int limit = 500)
        {
            var logs = await _context.RouteLogs
                .OrderByDescending(r => r.Timestamp)
                .Take(limit)
                .Select(r => new RoutePointDto
                {
                    Lat = r.Lat,
                    Lng = r.Lng,
                    Timestamp = r.Timestamp
                })
                .ToListAsync();

            return Ok(logs);
        }

        /// <summary>MỚI: Tạo dữ liệu route giả để test UI khi chưa có app thật đang chạy.</summary>
        [HttpPost("seed-demo")]
        public async Task<IActionResult> SeedDemoRoutes()
        {
            var rand = new Random();
            var now = DateTime.UtcNow;

            // Trung tâm HCM
            var centerLat = 10.7769;
            var centerLng = 106.7009;

            var platforms = new[] { "Android", "iOS", "Windows" };
            var names = new[] { "Pixel 7", "iPhone 14 Pro", "Samsung Galaxy S23", "Xiaomi 13", "iPhone 15" };

            for (int d = 0; d < 5; d++)
            {
                var devId = $"demo-route-{Guid.NewGuid().ToString()[..8]}";
                var platform = platforms[rand.Next(platforms.Length)];
                var deviceName = names[rand.Next(names.Length)];

                // Tạo thông tin thiết bị qua PlaybackLog heartbeat
                _context.PlaybackLogs.Add(new PlaybackLog
                {
                    POIId = 0,
                    TriggerType = "heartbeat",
                    TriggeredAt = now,
                    DeviceId = devId,
                    DeviceName = deviceName,
                    Platform = platform,
                    IpAddress = $"192.168.1.{rand.Next(2, 250)}",
                    UserName = $"Khách_demo{d + 1}"
                });

                // Tạo chuỗi 8-15 điểm di chuyển cách nhau vài trăm mét
                var numPoints = rand.Next(8, 16);
                var lat = centerLat + (rand.NextDouble() - 0.5) * 0.02;
                var lng = centerLng + (rand.NextDouble() - 0.5) * 0.02;
                var baseTime = now.AddHours(-rand.Next(1, 72));

                for (int i = 0; i < numPoints; i++)
                {
                    lat += (rand.NextDouble() - 0.5) * 0.003;
                    lng += (rand.NextDouble() - 0.5) * 0.003;

                    _context.RouteLogs.Add(new RouteLog
                    {
                        DeviceId = devId,
                        Lat = lat,
                        Lng = lng,
                        Timestamp = baseTime.AddMinutes(i * rand.Next(3, 12))
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã tạo dữ liệu route demo cho 5 thiết bị" });
        }

        // Haversine formula tính khoảng cách (km)
        private static double Haversine(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLng = (lng2 - lng1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }

    // Request/Response DTOs
    public class RouteLogRequest
    {
        public int? UserId { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string? DeviceId { get; set; }
    }

    public class RoutePointDto
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class RouteUserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public List<RoutePointDto> Points { get; set; } = new();
        public double TotalKm { get; set; }
    }

    public class RouteDeviceDto
    {
        public string DeviceId { get; set; } = "";
        public string DeviceIdShort { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string Platform { get; set; } = "";
        public string UserName { get; set; } = "";
        public List<RoutePointDto> Points { get; set; } = new();
        public double TotalKm { get; set; }
    }
}

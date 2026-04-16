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

        /// <summary>App gọi để lưu vị trí người dùng</summary>
        [HttpPost("log")]
        public async Task<IActionResult> LogLocation([FromBody] RouteLogRequest request)
        {
            var log = new RouteLog
            {
                UserId = request.UserId > 0 ? request.UserId : null,
                Lat = request.Lat,
                Lng = request.Lng,
                DeviceId = request.DeviceId,
                Timestamp = DateTime.UtcNow
            };

            _context.RouteLogs.Add(log);
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>Admin: lấy toàn bộ tuyến đường của 1 user theo phone</summary>
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

            // Tính tổng quãng đường (km)
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
        public int UserId { get; set; }
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
}

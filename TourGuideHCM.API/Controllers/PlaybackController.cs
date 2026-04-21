using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Controllers;

[ApiController]
[Route("api/playback")]
public class PlaybackController : ControllerBase
{
    private readonly AppDbContext _context;

    public PlaybackController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lịch sử nghe cho Heatmap — dùng PlaybackLog (có DeviceId) thay vì PlaybackHistory.
    /// Trả về cả DeviceId để admin đếm unique thiết bị.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 2000, [FromQuery] int days = 0)
    {
        var query = _context.PlaybackLogs
            .Include(x => x.POI)
            .Where(x => x.POIId > 0
                     && x.TriggerType != "heartbeat"
                     && x.TriggerType != "online")
            .AsQueryable();

        if (days > 0)
        {
            var from = DateTime.UtcNow.AddDays(-days);
            query = query.Where(x => x.TriggeredAt >= from);
        }

        var data = await query
            .OrderByDescending(x => x.TriggeredAt)
            .Take(limit)
            .Select(x => new HeatHistoryItem
            {
                PoiName = x.POI != null ? x.POI.Name : $"POI_{x.POIId}",
                UserId = x.UserId ?? 0,
                DeviceId = x.DeviceId ?? "",
                DeviceName = x.DeviceName ?? "",
                Platform = x.Platform ?? "",
                TriggeredAt = x.TriggeredAt
            })
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>
    /// [DEPRECATED] Lịch sử nghe theo SĐT — vẫn giữ cho khả năng tương thích ngược.
    /// App mới không còn đăng nhập, dùng /by-device thay thế.
    /// </summary>
    [HttpGet("user")]
    public async Task<IActionResult> GetByPhone([FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest("Vui lòng nhập số điện thoại");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == phone.Trim());

        if (user == null)
            return NotFound(new { message = "Không tìm thấy người dùng với số ĐT này" });

        var history = await _context.PlaybackLogs
            .Where(x => x.UserId == user.Id)
            .Include(x => x.POI)
            .OrderByDescending(x => x.TriggeredAt)
            .Select(x => new HeatHistoryItem
            {
                PoiName = x.POI != null ? x.POI.Name : $"POI_{x.POIId}",
                UserId = x.UserId ?? 0,
                DeviceId = x.DeviceId ?? "",
                DeviceName = x.DeviceName ?? "",
                Platform = x.Platform ?? "",
                TriggeredAt = x.TriggeredAt
            })
            .ToListAsync();

        return Ok(new
        {
            fullName = user.FullName ?? user.Username,
            history
        });
    }

    /// <summary>
    /// MỚI: Lấy lịch sử nghe của 1 thiết bị (theo DeviceId).
    /// </summary>
    [HttpGet("by-device")]
    public async Task<IActionResult> GetByDevice([FromQuery] string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return BadRequest(new { message = "Vui lòng nhập DeviceId" });

        var history = await _context.PlaybackLogs
            .Where(x => x.DeviceId == deviceId
                     && x.POIId > 0
                     && x.TriggerType != "heartbeat"
                     && x.TriggerType != "online")
            .Include(x => x.POI)
            .OrderByDescending(x => x.TriggeredAt)
            .Select(x => new HeatHistoryItem
            {
                PoiName = x.POI != null ? x.POI.Name : $"POI_{x.POIId}",
                UserId = x.UserId ?? 0,
                DeviceId = x.DeviceId ?? "",
                DeviceName = x.DeviceName ?? "",
                Platform = x.Platform ?? "",
                TriggeredAt = x.TriggeredAt
            })
            .ToListAsync();

        // Lấy tên thiết bị mới nhất để hiển thị
        var deviceName = history.FirstOrDefault()?.DeviceName ?? "Thiết bị ẩn danh";
        var platform = history.FirstOrDefault()?.Platform ?? "?";

        return Ok(new
        {
            deviceId,
            deviceName,
            platform,
            count = history.Count,
            history
        });
    }

    /// <summary>Lịch sử dạng bảng cho trang History — có filter ngày</summary>
    [HttpGet("table")]
    public async Task<IActionResult> GetTable([FromQuery] int limit = 1000, [FromQuery] int days = 0)
    {
        var query = _context.PlaybackLogs
            .Include(x => x.User)
            .Include(x => x.POI)
            .Where(x => x.POIId > 0
                     && x.TriggerType != "heartbeat"
                     && x.TriggerType != "online")
            .AsQueryable();

        if (days > 0)
        {
            var from = DateTime.UtcNow.AddDays(-days);
            query = query.Where(x => x.TriggeredAt >= from);
        }

        var data = await query
            .OrderByDescending(x => x.TriggeredAt)
            .Take(limit)
            .Select(x => new PlaybackHistoryDto
            {
                User = x.UserName ?? (x.User != null ? (x.User.FullName ?? x.User.Username ?? $"User_{x.UserId}") : "Ẩn danh"),
                Phone = x.User != null ? x.User.Phone : null,
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                Platform = x.Platform,
                Poi = x.POI != null ? x.POI.Name : $"POI_{x.POIId}",
                Time = x.TriggeredAt,
                Duration = x.DurationSeconds,
                TriggerType = x.TriggerType
            })
            .ToListAsync();

        return Ok(data);
    }

    // Endpoint POST giữ nguyên cho khả năng tương thích — nhưng khuyến khích app dùng /api/analytics/playback
    [HttpPost]
    public async Task<IActionResult> LogPlayback([FromBody] PlaybackLogDto dto)
    {
        if (dto.POIId <= 0) return Ok();

        var log = new PlaybackHistory
        {
            UserId = dto.UserId,
            POIId = dto.POIId,
            DurationSeconds = dto.DurationSeconds,
            TriggeredAt = DateTime.UtcNow,
            TriggerType = dto.TriggerType
        };
        _context.PlaybackHistories.Add(log);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class HeatHistoryItem
{
    public string PoiName { get; set; } = "";
    public int UserId { get; set; }
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public string Platform { get; set; } = "";
    public DateTime TriggeredAt { get; set; }
}

public class PlaybackLogDto
{
    public int UserId { get; set; }
    public int POIId { get; set; }
    public int? DurationSeconds { get; set; }
    public string? TriggerType { get; set; } = "app";
}

public class PlaybackHistoryDto
{
    public string User { get; set; } = "";
    public string? Phone { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
    public string Poi { get; set; } = "";
    public DateTime Time { get; set; }
    public double? Duration { get; set; }
    public string? TriggerType { get; set; }
}

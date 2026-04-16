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

    /// <summary>Lịch sử nghe cho heatmap — có filter theo ngày</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 2000, [FromQuery] int days = 0)
    {
        var query = _context.PlaybackHistories
            .Include(x => x.POI)
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
                UserId = x.UserId,
                TriggeredAt = x.TriggeredAt
            })
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>Lịch sử nghe theo SĐT người dùng</summary>
    [HttpGet("user")]
    public async Task<IActionResult> GetByPhone([FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest("Vui lòng nhập số điện thoại");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == phone.Trim());

        if (user == null)
            return NotFound(new { message = "Không tìm thấy người dùng với số ĐT này" });

        var history = await _context.PlaybackHistories
            .Where(x => x.UserId == user.Id)
            .Include(x => x.POI)
            .OrderByDescending(x => x.TriggeredAt)
            .Select(x => new HeatHistoryItem
            {
                PoiName = x.POI != null ? x.POI.Name : $"POI_{x.POIId}",
                UserId = x.UserId,
                TriggeredAt = x.TriggeredAt
            })
            .ToListAsync();

        return Ok(new
        {
            fullName = user.FullName ?? user.Username,
            history
        });
    }

    /// <summary>Lịch sử dạng bảng cho trang History — có filter ngày</summary>
    [HttpGet("table")]
    public async Task<IActionResult> GetTable([FromQuery] int limit = 1000, [FromQuery] int days = 0)
    {
        var query = _context.PlaybackHistories
            .Include(x => x.User)
            .Include(x => x.POI)
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
                User = x.User != null ? (x.User.FullName ?? x.User.Username ?? $"User_{x.UserId}") : "Ẩn danh",
                Phone = x.User != null ? x.User.Phone : null,
                Poi = x.POI != null ? x.POI.Name : $"POI_{x.POIId}",
                Time = x.TriggeredAt,
                Duration = x.DurationSeconds,
                TriggerType = x.TriggerType
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> LogPlayback([FromBody] PlaybackLogDto dto)
    {
        if (dto.UserId <= 0 || dto.POIId <= 0) return Ok();

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

public class HeatHistoryItem { public string PoiName { get; set; } = ""; public int UserId { get; set; } public DateTime TriggeredAt { get; set; } }
public class PlaybackLogDto { public int UserId { get; set; } public int POIId { get; set; } public int? DurationSeconds { get; set; } public string? TriggerType { get; set; } = "app"; }
public class PlaybackHistoryDto
{
    public string User { get; set; } = "";
    public string? Phone { get; set; }
    public string Poi { get; set; } = "";
    public DateTime Time { get; set; }
    public double? Duration { get; set; }
    public string? TriggerType { get; set; }
}

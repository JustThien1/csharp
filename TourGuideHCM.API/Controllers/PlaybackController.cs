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

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var data = await _context.PlaybackHistories
            .Include(x => x.User)
            .Include(x => x.POI)
            .OrderByDescending(x => x.TriggeredAt)
            .Take(200)
            .Select(x => new PlaybackHistoryDto
            {
                User = x.User != null
                    ? (x.User.Username ?? x.User.FullName ?? $"User_{x.UserId}")
                    : "Ẩn danh",
                Poi = x.POI != null ? x.POI.Name : $"POI_{x.POIId}",
                Time = x.TriggeredAt,
                Duration = x.DurationSeconds
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> LogPlayback([FromBody] PlaybackLogDto dto)
    {
        if (dto.UserId <= 0 || dto.POIId <= 0)
            return BadRequest("UserId và POIId là bắt buộc");

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

// DTOs - đặt ở cuối file
public class PlaybackLogDto
{
    public int UserId { get; set; }
    public int POIId { get; set; }
    public int? DurationSeconds { get; set; }
    public string? TriggerType { get; set; } = "app";
}

public class PlaybackHistoryDto
{
    public string User { get; set; } = string.Empty;
    public string Poi { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public double? Duration { get; set; }
}
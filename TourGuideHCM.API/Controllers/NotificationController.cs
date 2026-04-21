using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUser;

    public NotificationController(AppDbContext context, CurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Danh sách thông báo của user hiện tại.
    /// Trả 50 cái gần nhất (Unread ưu tiên trước).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] int limit = 50)
    {
        var userId = _currentUser.UserId;
        if (userId == 0) return Unauthorized();

        var list = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.IsRead)               // Unread trước
            .ThenByDescending(n => n.CreatedAt)
            .Take(Math.Min(limit, 100))
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                RelatedPoiId = n.RelatedPoiId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Số thông báo chưa đọc — cho badge.</summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = _currentUser.UserId;
        if (userId == 0) return Unauthorized();

        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Ok(new { count });
    }

    /// <summary>Đánh dấu 1 notification đã đọc.</summary>
    [HttpPost("{id}/mark-read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = _currentUser.UserId;
        var notif = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notif == null) return NotFound();

        if (!notif.IsRead)
        {
            notif.IsRead = true;
            notif.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    /// <summary>Đánh dấu tất cả đã đọc.</summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _currentUser.UserId;
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        return Ok(new { markedCount = unread.Count });
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public int? RelatedPoiId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Controllers;

/// <summary>
/// Admin duyệt POI do saler tạo.
/// Approve / Reject / Lock / Unlock — mỗi action gửi notification cho saler.
/// </summary>
[ApiController]
[Route("api/poi-approval")]
public class POIApprovalController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUser;

    public POIApprovalController(AppDbContext context, CurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Danh sách POI chờ duyệt (PendingReview) hoặc đã bị khoá (Locked) hoặc bị từ chối (Rejected).
    /// Dùng query param ?status= để filter. Mặc định: chỉ Pending.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] string status = "PendingReview")
    {
        var query = _context.POIs
            .Include(p => p.Category)
            .Include(p => p.CreatedBy)
            .Where(p => p.IsActive);

        query = status.ToLower() switch
        {
            "all" => query.Where(p => p.ReviewStatus != "Approved"),
            "pending" or "pendingreview" => query.Where(p => p.ReviewStatus == "PendingReview"),
            "rejected" => query.Where(p => p.ReviewStatus == "Rejected"),
            "locked" => query.Where(p => p.ReviewStatus == "Locked"),
            "approved" => query.Where(p => p.ReviewStatus == "Approved"),
            _ => query.Where(p => p.ReviewStatus == "PendingReview")
        };

        var list = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ApprovalItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Address = p.Address,
                Lat = p.Lat,
                Lng = p.Lng,
                Radius = p.Radius,
                NarrationText = p.NarrationText,
                Language = p.Language,
                ImageUrl = p.ImageUrl,
                CategoryName = p.Category != null ? p.Category.Name : "",
                ReviewStatus = p.ReviewStatus,
                RejectionReason = p.RejectionReason,
                CreatedAt = p.CreatedAt,
                CreatedByUsername = p.CreatedBy != null ? p.CreatedBy.Username : "(không rõ)",
                CreatedByFullName = p.CreatedBy != null ? p.CreatedBy.FullName : null,
                Audios = p.Audios.Select(a => new AudioPreviewDto
                {
                    Id = a.Id,
                    Language = a.Language ?? "vi",
                    AudioUrl = a.AudioUrl ?? "",
                    DurationSeconds = a.DurationSeconds,
                    Description = a.Description ?? ""
                }).ToList()
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Số POI chờ duyệt — cho badge NavMenu admin.</summary>
    [HttpGet("pending-count")]
    public async Task<IActionResult> GetPendingCount()
    {
        var count = await _context.POIs
            .CountAsync(p => p.IsActive && p.ReviewStatus == "PendingReview");
        return Ok(new { count });
    }

    /// <summary>Admin duyệt POI → chuyển sang Approved + notify saler.</summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var poi = await _context.POIs.FindAsync(id);
        if (poi == null) return NotFound();

        poi.ReviewStatus = "Approved";
        poi.RejectionReason = null;
        poi.ReviewedAt = DateTime.UtcNow;
        poi.ReviewedByUserId = _currentUser.UserId > 0 ? _currentUser.UserId : null;

        if (poi.CreatedByUserId.HasValue)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = poi.CreatedByUserId.Value,
                Type = "PoiApproved",
                Title = "POI đã được duyệt",
                Message = $"POI '{poi.Name}' của bạn đã được duyệt và đang hiển thị cho người dùng.",
                RelatedPoiId = poi.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Đã duyệt POI '{poi.Name}'" });
    }

    /// <summary>Admin từ chối POI → Rejected + notify saler kèm lý do.</summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] ReasonRequest req)
    {
        var poi = await _context.POIs.FindAsync(id);
        if (poi == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { message = "Vui lòng nhập lý do từ chối" });

        poi.ReviewStatus = "Rejected";
        poi.RejectionReason = req.Reason;
        poi.ReviewedAt = DateTime.UtcNow;
        poi.ReviewedByUserId = _currentUser.UserId > 0 ? _currentUser.UserId : null;

        if (poi.CreatedByUserId.HasValue)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = poi.CreatedByUserId.Value,
                Type = "PoiRejected",
                Title = "POI bị từ chối",
                Message = $"POI '{poi.Name}' bị từ chối. Lý do: {req.Reason}",
                RelatedPoiId = poi.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Đã từ chối POI '{poi.Name}'" });
    }

    /// <summary>Admin khoá POI đã Approved → Locked + notify saler.</summary>
    [HttpPost("{id}/lock")]
    public async Task<IActionResult> Lock(int id, [FromBody] ReasonRequest req)
    {
        var poi = await _context.POIs.FindAsync(id);
        if (poi == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { message = "Vui lòng nhập lý do khoá" });

        poi.ReviewStatus = "Locked";
        poi.RejectionReason = req.Reason;
        poi.ReviewedAt = DateTime.UtcNow;
        poi.ReviewedByUserId = _currentUser.UserId > 0 ? _currentUser.UserId : null;

        if (poi.CreatedByUserId.HasValue)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = poi.CreatedByUserId.Value,
                Type = "PoiLocked",
                Title = "POI bị khoá",
                Message = $"POI '{poi.Name}' đã bị khoá. Lý do: {req.Reason}",
                RelatedPoiId = poi.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Đã khoá POI '{poi.Name}'" });
    }

    /// <summary>Admin mở khoá POI bị Locked → Approved trở lại.</summary>
    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> Unlock(int id)
    {
        var poi = await _context.POIs.FindAsync(id);
        if (poi == null) return NotFound();

        if (poi.ReviewStatus != "Locked")
            return BadRequest(new { message = "POI không ở trạng thái bị khoá" });

        poi.ReviewStatus = "Approved";
        poi.RejectionReason = null;
        poi.ReviewedAt = DateTime.UtcNow;

        if (poi.CreatedByUserId.HasValue)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = poi.CreatedByUserId.Value,
                Type = "PoiApproved",
                Title = "POI đã được mở khoá",
                Message = $"POI '{poi.Name}' đã được mở khoá và hoạt động trở lại.",
                RelatedPoiId = poi.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Đã mở khoá POI '{poi.Name}'" });
    }

    public class ReasonRequest
    {
        public string Reason { get; set; } = "";
    }

    public class ApprovalItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Address { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Radius { get; set; }
        public string? NarrationText { get; set; }
        public string Language { get; set; } = "vi";
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = "";
        public string ReviewStatus { get; set; } = "";
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUsername { get; set; } = "";
        public string? CreatedByFullName { get; set; }
        public List<AudioPreviewDto> Audios { get; set; } = new();
    }

    public class AudioPreviewDto
    {
        public int Id { get; set; }
        public string Language { get; set; } = "";
        public string AudioUrl { get; set; } = "";
        public int DurationSeconds { get; set; }
        public string Description { get; set; } = "";
    }
}

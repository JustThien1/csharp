using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Controllers;

[ApiController]
[Route("api/duplicate-reports")]
public class DuplicateReportController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DuplicateDetectionService _detector;

    public DuplicateReportController(AppDbContext context, DuplicateDetectionService detector)
    {
        _context = context;
        _detector = detector;
    }

    /// <summary>
    /// Lấy danh sách report để admin duyệt.
    /// Mặc định chỉ trả Open (chưa resolve, chưa dismiss).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string status = "Open")
    {
        var query = _context.DuplicateReports
            .Include(r => r.PoiA)
            .Include(r => r.PoiB)
            .AsQueryable();

        query = status.ToLower() switch
        {
            "open" => query.Where(r => !r.IsDismissed && r.Status == "Open"),
            "resolved" => query.Where(r => r.Status == "Resolved"),
            "dismissed" => query.Where(r => r.IsDismissed),
            "all" => query,
            _ => query.Where(r => !r.IsDismissed && r.Status == "Open")
        };

        var result = await query
            .OrderByDescending(r => r.Level)    // Exact > High > Medium
            .ThenByDescending(r => r.CreatedAt)
            .Select(r => new DuplicateReportDto
            {
                Id = r.Id,
                Level = r.Level,
                NameSimilarity = r.NameSimilarity,
                DistanceMeters = r.DistanceMeters,
                Status = r.Status,
                IsDismissed = r.IsDismissed,
                Resolution = r.Resolution,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt,
                PoiA = r.PoiA == null ? null : new DuplicatePoiDto
                {
                    Id = r.PoiA.Id,
                    Name = r.PoiA.Name,
                    Address = r.PoiA.Address,
                    Lat = r.PoiA.Lat,
                    Lng = r.PoiA.Lng,
                    CategoryId = r.PoiA.CategoryId,
                    IsActive = r.PoiA.IsActive
                },
                PoiB = r.PoiB == null ? null : new DuplicatePoiDto
                {
                    Id = r.PoiB.Id,
                    Name = r.PoiB.Name,
                    Address = r.PoiB.Address,
                    Lat = r.PoiB.Lat,
                    Lng = r.PoiB.Lng,
                    CategoryId = r.PoiB.CategoryId,
                    IsActive = r.PoiB.IsActive
                }
            })
            .ToListAsync();

        return Ok(result);
    }

    /// <summary>Số lượng report Open — cho badge hiển thị trên NavMenu.</summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetOpenCount()
    {
        var count = await _context.DuplicateReports
            .CountAsync(r => !r.IsDismissed && r.Status == "Open");
        return Ok(new { count });
    }

    /// <summary>
    /// Quét toàn bộ DB tìm thêm các cặp trùng chưa được phát hiện.
    /// Tạo report cho các cặp mới.
    /// </summary>
    [HttpPost("scan")]
    public async Task<IActionResult> ScanAll()
    {
        var created = await _detector.ScanAllAsync();
        return Ok(new
        {
            message = $"Đã quét xong. Phát hiện {created} cặp trùng mới.",
            newReports = created
        });
    }

    /// <summary>
    /// Admin chọn "Giữ cả hai" — đánh dấu report đã resolved với ghi chú.
    /// </summary>
    [HttpPost("{id}/keep-both")]
    public async Task<IActionResult> KeepBoth(int id, [FromBody] ResolveRequest? req)
    {
        var report = await _context.DuplicateReports
            .Include(r => r.PoiA)
            .Include(r => r.PoiB)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();

        report.Status = "Resolved";
        report.Resolution = "KeepBoth";
        report.ResolutionNote = req?.Note;
        report.ResolvedAt = DateTime.UtcNow;
        report.ResolvedBy = req?.AdminName;

        // Đưa cả 2 POI về Approved
        if (report.PoiA != null) report.PoiA.ReviewStatus = "Approved";
        if (report.PoiB != null) report.PoiB.ReviewStatus = "Approved";

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã giữ cả hai POI." });
    }

    /// <summary>
    /// Admin chọn xoá 1 trong 2 POI.
    /// keepId = ID của POI muốn giữ, cái còn lại sẽ bị soft-delete.
    /// Playback log của POI bị xoá sẽ chuyển sang POI được giữ.
    /// </summary>
    [HttpPost("{id}/merge")]
    public async Task<IActionResult> Merge(int id, [FromBody] MergeRequest req)
    {
        var report = await _context.DuplicateReports
            .Include(r => r.PoiA)
            .Include(r => r.PoiB)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();
        if (report.PoiA == null || report.PoiB == null)
            return BadRequest(new { message = "POI trong report không còn tồn tại" });

        // Xác định giữ cái nào
        POI keep, remove;
        if (req.KeepId == report.PoiAId)
        {
            keep = report.PoiA;
            remove = report.PoiB;
        }
        else if (req.KeepId == report.PoiBId)
        {
            keep = report.PoiB;
            remove = report.PoiA;
        }
        else
        {
            return BadRequest(new { message = "KeepId phải là 1 trong 2 POI của report" });
        }

        // Chuyển playback log sang POI được giữ
        await _context.PlaybackLogs
            .Where(l => l.POIId == remove.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.POIId, keep.Id));

        // Soft-delete POI còn lại
        remove.IsActive = false;

        keep.ReviewStatus = "Approved";

        // Mark report resolved
        report.Status = "Resolved";
        report.Resolution = req.KeepId == report.PoiAId ? "DeletedB" : "DeletedA";
        report.ResolutionNote = req.Note;
        report.ResolvedAt = DateTime.UtcNow;
        report.ResolvedBy = req.AdminName;

        // Các report khác liên quan đến POI bị xoá → tự động dismiss
        var relatedReports = await _context.DuplicateReports
            .Where(r => r.Id != id
                     && !r.IsDismissed
                     && r.Status == "Open"
                     && (r.PoiAId == remove.Id || r.PoiBId == remove.Id))
            .ToListAsync();

        foreach (var rel in relatedReports)
        {
            rel.IsDismissed = true;
            rel.DismissedAt = DateTime.UtcNow;
            rel.ResolutionNote = $"Tự động bỏ qua vì POI #{remove.Id} đã bị gộp trong report #{id}";
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = $"Đã giữ POI '{keep.Name}', xoá POI '{remove.Name}'",
            relatedDismissed = relatedReports.Count
        });
    }

    /// <summary>
    /// Admin "bỏ qua" report — không phải trùng, không cần làm gì cả.
    /// Khác với Resolved: Dismissed nghĩa là "sai phát hiện" (false positive).
    /// </summary>
    [HttpPost("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(int id, [FromBody] ResolveRequest? req)
    {
        var report = await _context.DuplicateReports
            .Include(r => r.PoiA)
            .Include(r => r.PoiB)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();

        report.IsDismissed = true;
        report.DismissedAt = DateTime.UtcNow;
        report.ResolutionNote = req?.Note;
        report.ResolvedBy = req?.AdminName;

        // Đưa cả 2 POI về Approved
        if (report.PoiA != null) report.PoiA.ReviewStatus = "Approved";
        if (report.PoiB != null) report.PoiB.ReviewStatus = "Approved";

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã bỏ qua cảnh báo." });
    }

    // ====================== DTOs ======================
    public class DuplicateReportDto
    {
        public int Id { get; set; }
        public string Level { get; set; } = "";
        public double NameSimilarity { get; set; }
        public double DistanceMeters { get; set; }
        public string Status { get; set; } = "";
        public bool IsDismissed { get; set; }
        public string? Resolution { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DuplicatePoiDto? PoiA { get; set; }
        public DuplicatePoiDto? PoiB { get; set; }
    }

    public class DuplicatePoiDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int CategoryId { get; set; }
        public bool IsActive { get; set; }
    }

    public class ResolveRequest
    {
        public string? Note { get; set; }
        public string? AdminName { get; set; }
    }

    public class MergeRequest
    {
        public int KeepId { get; set; }
        public string? Note { get; set; }
        public string? AdminName { get; set; }
    }
}

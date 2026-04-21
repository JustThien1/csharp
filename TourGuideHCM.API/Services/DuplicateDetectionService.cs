using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Helpers;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services;

/// <summary>
/// Phát hiện POI trùng lặp và tạo DuplicateReport để admin duyệt.
/// KHÔNG tự động xoá/gộp — mọi quyết định thuộc về admin.
/// 
/// 3 mức độ:
/// - Exact:  tên giống ≥95% + cách ≤20m → chắc chắn trùng
/// - High:   tên giống ≥85% + cách ≤50m → khả năng cao
/// - Medium: tên giống ≥70% + cách ≤100m → cần kiểm tra
/// </summary>
public class DuplicateDetectionService
{
    private readonly AppDbContext _context;

    public const double EXACT_NAME_THRESHOLD = 0.95;
    public const double HIGH_NAME_THRESHOLD = 0.85;
    public const double MEDIUM_NAME_THRESHOLD = 0.70;

    public const double EXACT_DISTANCE_M = 20;
    public const double HIGH_DISTANCE_M = 50;
    public const double MEDIUM_DISTANCE_M = 100;

    public DuplicateDetectionService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Kiểm tra 1 POI sắp được tạo/đã tạo có trùng với POI nào khác không.
    /// Nếu có và chưa có report nào tồn tại → tự động tạo DuplicateReport.
    /// Trả về danh sách các cặp bị phát hiện (để UI thông báo admin).
    /// </summary>
    public async Task<List<DuplicateMatch>> CheckAndReportAsync(int poiId)
    {
        var target = await _context.POIs.FirstOrDefaultAsync(p => p.Id == poiId);
        if (target == null) return new();

        var candidates = await _context.POIs
            .Where(p => p.IsActive && p.Id != poiId)
            .ToListAsync();

        var matches = new List<DuplicateMatch>();

        foreach (var other in candidates)
        {
            var match = Evaluate(target, other);
            if (match == null) continue;

            // Tránh tạo report trùng: kiểm tra đã có record nào cho cặp (target, other) chưa
            var exists = await _context.DuplicateReports.AnyAsync(r =>
                ((r.PoiAId == target.Id && r.PoiBId == other.Id) ||
                 (r.PoiAId == other.Id && r.PoiBId == target.Id)) &&
                !r.IsDismissed && r.Status == "Open");

            if (!exists)
            {
                _context.DuplicateReports.Add(new DuplicateReport
                {
                    PoiAId = target.Id,
                    PoiBId = other.Id,
                    NameSimilarity = match.NameSimilarity,
                    DistanceMeters = match.DistanceMeters,
                    Level = match.Level,
                    Status = "Open"
                });

                // Đánh dấu POI mới là PendingReview
                target.ReviewStatus = "PendingReview";
            }

            matches.Add(match);
        }

        if (matches.Any())
            await _context.SaveChangesAsync();

        return matches;
    }

    /// <summary>
    /// Quét toàn bộ DB tìm tất cả các cặp trùng (O(n²) — dùng khi admin bấm "Scan toàn bộ").
    /// Tạo report cho các cặp chưa có.
    /// </summary>
    public async Task<int> ScanAllAsync()
    {
        var pois = await _context.POIs
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var existingPairs = await _context.DuplicateReports
            .Where(r => !r.IsDismissed && r.Status == "Open")
            .Select(r => new { r.PoiAId, r.PoiBId })
            .ToListAsync();

        // HashSet các cặp (min, max) để so sánh nhanh
        var existingSet = existingPairs
            .Select(p => (Math.Min(p.PoiAId, p.PoiBId), Math.Max(p.PoiAId, p.PoiBId)))
            .ToHashSet();

        int created = 0;

        for (int i = 0; i < pois.Count; i++)
        {
            for (int j = i + 1; j < pois.Count; j++)
            {
                var a = pois[i];
                var b = pois[j];
                var key = (Math.Min(a.Id, b.Id), Math.Max(a.Id, b.Id));
                if (existingSet.Contains(key)) continue;

                var match = Evaluate(a, b);
                if (match == null) continue;

                _context.DuplicateReports.Add(new DuplicateReport
                {
                    PoiAId = a.Id,
                    PoiBId = b.Id,
                    NameSimilarity = match.NameSimilarity,
                    DistanceMeters = match.DistanceMeters,
                    Level = match.Level,
                    Status = "Open"
                });
                created++;
            }
        }

        if (created > 0)
            await _context.SaveChangesAsync();

        return created;
    }

    /// <summary>Đánh giá 1 cặp POI — trả về DuplicateMatch nếu trùng, null nếu không.</summary>
    private static DuplicateMatch? Evaluate(POI a, POI b)
    {
        var nameSim = StringHelper.Similarity(a.Name, b.Name);
        var distance = HaversineHelper.CalculateDistance(a.Lat, a.Lng, b.Lat, b.Lng);

        string? level = null;
        if (nameSim >= EXACT_NAME_THRESHOLD && distance <= EXACT_DISTANCE_M)
            level = "Exact";
        else if (nameSim >= HIGH_NAME_THRESHOLD && distance <= HIGH_DISTANCE_M)
            level = "High";
        else if (nameSim >= MEDIUM_NAME_THRESHOLD && distance <= MEDIUM_DISTANCE_M)
            level = "Medium";

        if (level == null) return null;

        return new DuplicateMatch
        {
            ExistingPoi = b,
            NameSimilarity = Math.Round(nameSim, 3),
            DistanceMeters = Math.Round(distance, 1),
            Level = level
        };
    }
}

public class DuplicateMatch
{
    public POI ExistingPoi { get; set; } = null!;
    public double NameSimilarity { get; set; }
    public double DistanceMeters { get; set; }
    public string Level { get; set; } = "";
}

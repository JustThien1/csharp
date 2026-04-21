namespace TourGuideHCM.API.Models
{
    /// <summary>
    /// Báo cáo một cặp POI bị nghi ngờ trùng lặp.
    /// Được tạo tự động khi admin thêm POI trùng, hoặc khi admin chạy scan toàn bộ DB.
    /// Admin sẽ review và chọn resolution.
    /// </summary>
    public class DuplicateReport
    {
        public int Id { get; set; }

        // Cặp POI bị nghi trùng — PoiAId thường là POI mới/có ID nhỏ hơn
        public int PoiAId { get; set; }
        public int PoiBId { get; set; }

        public POI? PoiA { get; set; }
        public POI? PoiB { get; set; }

        // Kết quả phân tích
        public double NameSimilarity { get; set; }     // 0..1 — Levenshtein similarity
        public double DistanceMeters { get; set; }     // Khoảng cách giữa 2 POI
        public string Level { get; set; } = "Medium";  // Exact | High | Medium

        // Trạng thái xử lý
        public string Status { get; set; } = "Open";   // Open | Resolved | Dismissed
        public string? Resolution { get; set; }        // KeepBoth | Merged | DeletedA | DeletedB | EditedA | EditedB
        public string? ResolutionNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedBy { get; set; }        // Tên admin (để sau khi có auth admin)

        // Đánh dấu "đã bỏ qua" riêng để phân biệt với "đã xử lý"
        public bool IsDismissed { get; set; } = false;
        public DateTime? DismissedAt { get; set; }
    }
}

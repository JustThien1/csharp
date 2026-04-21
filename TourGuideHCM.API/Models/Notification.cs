namespace TourGuideHCM.API.Models
{
    /// <summary>
    /// Thông báo trong hệ thống, gửi cho một user cụ thể.
    /// Ví dụ: "POI 'Nhà thờ Đức Bà' của bạn đã được duyệt",
    ///        "Saler Nguyễn Văn A vừa tạo POI mới chờ duyệt".
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }

        /// <summary>User nhận thông báo.</summary>
        public int UserId { get; set; }
        public User? User { get; set; }

        /// <summary>
        /// Loại thông báo để UI format:
        /// - "PoiApproved" / "PoiRejected" / "PoiLocked" / "PoiCreated" / "AccountLocked" / "System"
        /// </summary>
        public string Type { get; set; } = "System";

        public string Title { get; set; } = "";
        public string Message { get; set; } = "";

        /// <summary>POI liên quan (optional) — để UI click vào navigate tới POI đó.</summary>
        public int? RelatedPoiId { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}

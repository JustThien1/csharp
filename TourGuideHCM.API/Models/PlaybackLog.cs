namespace TourGuideHCM.API.Models
{
    public class PlaybackLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }           // null = ẩn danh

        /// <summary>
        /// ID của POI được nghe. NULL nghĩa là log heartbeat/online
        /// (user mở app nhưng chưa nghe POI nào).
        /// Trước đây dùng 0 làm sentinel → bị FK constraint fail vì không có POI Id=0.
        /// </summary>
        public int? POIId { get; set; }

        public DateTime PlayedAt { get; set; }
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public double? DurationSeconds { get; set; }   // thời gian nghe
        public string? TriggerType { get; set; }       // "geofence", "qr", "manual", "online", "heartbeat"

        // ====================== MỞ RỘNG CHO MONITORING THẬT ======================
        public string? DeviceId { get; set; }       // Unique per thiết bị (GUID sinh trên app, lưu Preferences)
        public string? DeviceName { get; set; }     // Ví dụ: "Pixel 7", "iPhone 14", "Samsung SM-A515"
        public string? Platform { get; set; }       // "Android", "iOS", "Windows"
        public string? IpAddress { get; set; }      // IP của thiết bị (server ghi)
        public string? UserName { get; set; }       // Tên hiển thị (lấy từ User hoặc "Khách_XXXX")

        public User? User { get; set; }
        public POI? POI { get; set; }
    }
}

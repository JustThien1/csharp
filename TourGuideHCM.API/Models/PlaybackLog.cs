namespace TourGuideHCM.API.Models
{
    public class PlaybackLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }           // null = ẩn danh
        public int POIId { get; set; }
        public DateTime PlayedAt { get; set; }
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public double? DurationSeconds { get; set; }   // thời gian nghe
        public string? TriggerType { get; set; }       // "geofence", "qr", "manual"

        public User? User { get; set; }
        public POI? POI { get; set; }
    }
}
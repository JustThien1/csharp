namespace TourGuideHCM.API.Models
{
    public class TtsJob
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public string Language { get; set; } = "vi";
        public string Gender { get; set; } = "female";    // female / male
        public string Text { get; set; } = string.Empty;
        public double Speed { get; set; } = 1.0;

        public string Status { get; set; } = "Pending";   // Pending / Processing / Completed / Failed
        public string? AudioUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public POI? POI { get; set; }
    }
}

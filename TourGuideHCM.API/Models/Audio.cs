namespace TourGuideHCM.API.Models
{
    public class Audio
    {
        public int Id { get; set; }

        public int PoiId { get; set; }
        public POI? POI { get; set; }           // Navigation property

        public string Language { get; set; } = "vi";
        public string AudioUrl { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
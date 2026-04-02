namespace TourGuideHCM.API.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int POIId { get; set; }
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public POI? POI { get; set; }
    }
}
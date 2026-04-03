namespace TourGuideHCM.API.Models
{
    public class RouteLog
    {
        public int Id { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? DeviceId { get; set; }   // ẩn danh
    }
}
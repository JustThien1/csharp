namespace TourGuideHCM.Admin.Models;

public class PlaybackHistoryDto
{
    public string User { get; set; } = string.Empty;           // Tên user hoặc "Khách_xxxxxxxx"
    public string? Phone { get; set; }                          // Có thể null khi app không login
    public string? DeviceId { get; set; }                       // MỚI
    public string? DeviceName { get; set; }                     // MỚI: "Pixel 7", "iPhone 14"
    public string? Platform { get; set; }                       // MỚI: Android / iOS / Windows
    public string Poi { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public double? Duration { get; set; }
    public string? TriggerType { get; set; }                    // geofence / qr / manual
}

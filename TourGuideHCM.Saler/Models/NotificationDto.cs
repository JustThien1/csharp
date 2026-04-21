namespace TourGuideHCM.Saler.Models;

public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public int? RelatedPoiId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

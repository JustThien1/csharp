namespace TourGuideHCM.Admin.Models;

public class TtsJobDto
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public string Language { get; set; } = "vi";
    public string Text { get; set; } = "";
    public string? AudioUrl { get; set; }
    public string Status { get; set; } = "Pending";   // Pending, Processing, Completed, Failed
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; } = 0;
}
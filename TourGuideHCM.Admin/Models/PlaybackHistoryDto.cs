namespace TourGuideHCM.Admin.Models;

public class PlaybackHistoryDto
{
    public string User { get; set; } = string.Empty;
    public string Poi { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public double? Duration { get; set; }
}
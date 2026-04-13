public class PlaybackLogDto
{
    public int UserId { get; set; }
    public int POIId { get; set; }
    public int? DurationSeconds { get; set; }
    public string? TriggerType { get; set; } = "auto"; // manual / auto / background
}
namespace TourGuideHCM.Saler.Models;

public class AudioDto
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public string Language { get; set; } = "vi";
    public string AudioUrl { get; set; } = "";
    public int DurationSeconds { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

/// <summary>Request để convert text → TTS audio qua Google TTS.</summary>
public class TtsConvertRequest
{
    public int PoiId { get; set; }
    public string Text { get; set; } = "";
    public string Language { get; set; } = "vi";
    public string Gender { get; set; } = "female";
    public double Speed { get; set; } = 1.0;
}

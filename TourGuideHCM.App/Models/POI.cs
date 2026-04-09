namespace TourGuideHCM.App.Models;

public class Poi
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Radius { get; set; } = 100;
    public int Priority { get; set; } = 1;
    public string? AudioUrl { get; set; }
    public string? NarrationText { get; set; }
    public string Language { get; set; } = "vi";
    public string? ImageUrl { get; set; }
}
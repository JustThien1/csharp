namespace TourGuideHCM.Mobile.Models;

public class POI
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Radius { get; set; } = 100;

    public string? AudioUrl { get; set; }
    public string? NarrationText { get; set; }

    public string Language { get; set; } = "vi";
    public bool IsActive { get; set; } = true;

    // 🔥 nâng cấp
    public int Priority { get; set; } = 0;
    public string? ImageUrl { get; set; }
    public double TriggerRadius { get; set; } = 100; // mét
}
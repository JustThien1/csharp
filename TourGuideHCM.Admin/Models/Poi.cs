namespace TourGuideHCM.Admin.Models;

public class POI
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Radius { get; set; } = 100;
    public int Priority { get; set; } = 1;
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? NarrationText { get; set; }
    public string Language { get; set; } = "vi";
    public string? OpeningHours { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
}
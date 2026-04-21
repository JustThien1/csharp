namespace TourGuideHCM.Saler.Models;

public class PoiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";
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

    public string ReviewStatus { get; set; } = "PendingReview";
    public string? RejectionReason { get; set; }

    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public int CategoryId { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

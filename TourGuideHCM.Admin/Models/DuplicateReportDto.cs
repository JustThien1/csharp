namespace TourGuideHCM.Admin.Models;

public class DuplicateReportDto
{
    public int Id { get; set; }
    public string Level { get; set; } = "";        // Exact | High | Medium
    public double NameSimilarity { get; set; }
    public double DistanceMeters { get; set; }
    public string Status { get; set; } = "";       // Open | Resolved
    public bool IsDismissed { get; set; }
    public string? Resolution { get; set; }        // KeepBoth | DeletedA | DeletedB | ...
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DuplicatePoiDto? PoiA { get; set; }
    public DuplicatePoiDto? PoiB { get; set; }
}

public class DuplicatePoiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; }
}

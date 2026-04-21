namespace TourGuideHCM.Admin.Models;

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

public class ApprovalItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Radius { get; set; }
    public string? NarrationText { get; set; }
    public string Language { get; set; } = "vi";
    public string? ImageUrl { get; set; }
    public string CategoryName { get; set; } = "";
    public string ReviewStatus { get; set; } = "";
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByUsername { get; set; } = "";
    public string? CreatedByFullName { get; set; }
    public List<AudioPreviewDto> Audios { get; set; } = new();
}

public class AudioPreviewDto
{
    public int Id { get; set; }
    public string Language { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public int DurationSeconds { get; set; }
    public string Description { get; set; } = "";
}

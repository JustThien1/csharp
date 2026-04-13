using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourGuideHCM.API.Models;

public class PlaybackHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [Required]
    public int POIId { get; set; }

    [ForeignKey(nameof(POIId))]
    public virtual POI? POI { get; set; }

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    public int? DurationSeconds { get; set; }

    public string? TriggerType { get; set; }
}
using SQLite;

namespace TourGuideHCM.App.Models;

[Table("POIs")]
public class POI
{
    [PrimaryKey]
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

    // ── Lưu vào DB local ─────────────────────────────────────────────────────
    /// <summary>Người dùng đã thêm vào yêu thích</summary>
    public bool IsFavorite { get; set; } = false;

    // ── Runtime-only ─────────────────────────────────────────────────────────
    [Ignore] public string CategoryName { get; set; } = string.Empty;
    [Ignore] public double? DistanceMeters { get; set; }
    [Ignore] public bool IsNearby { get; set; }
    [Ignore] public bool IsHighlighted { get; set; }

    [Ignore]
    public string DistanceDisplay => DistanceMeters.HasValue
        ? DistanceMeters.Value < 1000
            ? $"{DistanceMeters.Value:F0}m"
            : $"{DistanceMeters.Value / 1000:F1}km"
        : string.Empty;

    [Ignore]
    public string ShortDescription => string.IsNullOrEmpty(Description) ? string.Empty
        : Description.Length > 100 ? Description[..100] + "…" : Description;

    [Ignore]
    public string TicketDisplay => TicketPrice is null or 0 ? "Miễn phí"
        : $"{TicketPrice:N0}đ";

    /// <summary>Icon yêu thích hiển thị trên UI</summary>
    [Ignore]
    public string FavoriteIcon => IsFavorite ? "❤️" : "🤍";

    /// <summary>Màu highlight khi POI là nearest</summary>
    [Ignore]
    public Color HighlightColor => IsHighlighted ? Color.FromArgb("#FF6F00") : Colors.Transparent;
}

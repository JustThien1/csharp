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

    // ── Runtime-only (không lưu DB) ─────────────────────────────────────────
    [Ignore] public string CategoryName { get; set; } = string.Empty;
    [Ignore] public double? DistanceMeters { get; set; }
    [Ignore] public bool IsNearby { get; set; }
    [Ignore] public bool IsHighlighted { get; set; }

    /// <summary>Emoji icon hiển thị ở card (gán từ HomeViewModel theo CategoryId)</summary>
    [Ignore] public string IconEmoji { get; set; } = "📍";

    /// <summary>Màu gradient bắt đầu cho card thumbnail</summary>
    [Ignore] public string GradientStart { get; set; } = "#667EEA";

    /// <summary>Màu gradient kết thúc cho card thumbnail</summary>
    [Ignore] public string GradientEnd { get; set; } = "#764BA2";

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

    /// <summary>Nhãn category ngắn — hiển thị ở card home</summary>
    [Ignore]
    public string CategoryLabel => CategoryId switch
    {
        1 => "Di tích",
        2 => "Ẩm thực",
        3 => "Mua sắm",
        4 => "Cafe",
        5 => "Quán ăn",
        6 => "Nhà hàng",
        7 => "Khách sạn",
        8 => "Công viên",
        _ => string.IsNullOrEmpty(CategoryName) ? "Địa điểm" : CategoryName
    };

    /// <summary>Thông tin mở cửa hiển thị (fallback nếu null)</summary>
    [Ignore]
    public string OpeningDisplay => string.IsNullOrWhiteSpace(OpeningHours)
        ? "Mở cửa cả ngày"
        : $"Mở {OpeningHours}";

    /// <summary>Trạng thái đông/vắng — dựa vào giờ hiện tại + giờ mở cửa đơn giản</summary>
    [Ignore]
    public string BusyStatus
    {
        get
        {
            // Nếu có OpeningHours dạng "07:00-17:00" thì check giờ hiện tại
            if (!string.IsNullOrWhiteSpace(OpeningHours) && OpeningHours.Contains('-'))
            {
                var parts = OpeningHours.Split('-');
                if (parts.Length == 2
                    && TimeSpan.TryParse(parts[0].Trim(), out var open)
                    && TimeSpan.TryParse(parts[1].Trim(), out var close))
                {
                    var now = DateTime.Now.TimeOfDay;
                    if (now < open || now > close) return "● Đóng";
                }
            }
            // Giả lập theo giờ cao điểm (11-13h, 17-20h → đông)
            var hour = DateTime.Now.Hour;
            if ((hour >= 11 && hour <= 13) || (hour >= 17 && hour <= 20))
                return "● Đông";
            if (hour >= 9 && hour <= 21)
                return "● Vắng";
            return "● Đóng";
        }
    }

    /// <summary>Màu cho badge BusyStatus</summary>
    [Ignore]
    public Color BusyColor => BusyStatus switch
    {
        "● Đóng" => Color.FromArgb("#C62828"),
        "● Đông" => Color.FromArgb("#E65100"),
        _ => Color.FromArgb("#2E7D32")
    };

    [Ignore]
    public Color BusyBgColor => BusyStatus switch
    {
        "● Đóng" => Color.FromArgb("#FFEBEE"),
        "● Đông" => Color.FromArgb("#FFF3E0"),
        _ => Color.FromArgb("#E8F5E9")
    };

    /// <summary>Icon yêu thích hiển thị trên UI</summary>
    [Ignore]
    public string FavoriteIcon => IsFavorite ? "❤️" : "🤍";

    /// <summary>Màu highlight khi POI là nearest</summary>
    [Ignore]
    public Color HighlightColor => IsHighlighted ? Color.FromArgb("#FF6F00") : Colors.Transparent;
}

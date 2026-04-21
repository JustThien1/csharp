using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TourGuideHCM.App.Models;

/// <summary>
/// POI đang nằm trong audio queue — chờ phát hoặc đang phát.
/// Được dùng bởi <see cref="Services.IAudioQueueService"/> và bind ra UI
/// (danh sách "Sắp tới", card "Đang phát") trong MapPage.
/// </summary>
public class QueuedPoi : INotifyPropertyChanged
{
    /// <summary>POI gốc (tên, toạ độ, audio url, …).</summary>
    public POI Poi { get; set; } = new();

    /// <summary>Khoảng cách từ user đến POI (mét) tại thời điểm enqueue.</summary>
    public double Distance { get; set; }

    /// <summary>Ngôn ngữ narration khi enqueue ("vi" / "en").</summary>
    public string Language { get; set; } = "vi";

    /// <summary>Thời điểm POI được đưa vào queue (UTC).</summary>
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;

    private bool _isPlaying;
    /// <summary>True = đây là POI đang được phát (Current); False = đang chờ.</summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying == value) return;
            _isPlaying = value;
            OnPropertyChanged();
        }
    }

    // ============ Helpers cho UI binding ============
    public string Name => Poi.Name;
    public string DistanceDisplay =>
        Distance >= 1000 ? $"{Distance / 1000:F1} km" : $"{Distance:F0} m";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

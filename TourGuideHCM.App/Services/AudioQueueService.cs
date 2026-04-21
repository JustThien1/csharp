using System.Collections.ObjectModel;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

/// <summary>
/// Implementation mặc định của <see cref="IAudioQueueService"/>.
/// - Xếp POI trong vùng vào queue theo khoảng cách tăng dần.
/// - Không thêm lại POI đã phát trong <see cref="_cooldown"/> (tránh spam).
/// - Khi POI hiện tại phát xong, tự động chuyển sang POI kế tiếp.
/// </summary>
public class AudioQueueService : IAudioQueueService
{
    private readonly INarrationService _narration;

    /// <summary>POI đã phát gần đây → không enqueue lại trong cooldown.</summary>
    private readonly Dictionary<int, DateTime> _recentlyPlayed = new();

    /// <summary>Thời gian chờ trước khi 1 POI có thể được phát lại.</summary>
    private readonly TimeSpan _cooldown = TimeSpan.FromMinutes(10);

    private readonly ObservableCollection<QueuedPoi> _queue = new();
    public ReadOnlyObservableCollection<QueuedPoi> Queue { get; }

    private QueuedPoi? _current;
    public QueuedPoi? Current
    {
        get => _current;
        private set
        {
            _current = value;
            RaiseChanged();
        }
    }

    public event EventHandler? QueueChanged;

    public AudioQueueService(INarrationService narration)
    {
        _narration = narration;
        Queue = new ReadOnlyObservableCollection<QueuedPoi>(_queue);

        // Khi audio hiện tại kết thúc → chuyển POI tiếp theo
        _narration.NarrationCompleted += OnNarrationCompleted;
    }

    // ====================== PUBLIC API ======================

    public void UpdateInRangePois(List<(POI poi, double distance)> poisInRange, string language)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // 1. Remove những item trong queue đã ra khỏi vùng
            var currentIds = poisInRange.Select(p => p.poi.Id).ToHashSet();
            var toRemove = _queue.Where(q => !currentIds.Contains(q.Poi.Id)).ToList();
            foreach (var r in toRemove) _queue.Remove(r);

            // 2. Thêm POI mới (chưa có trong queue, chưa là Current, chưa trong cooldown)
            foreach (var (poi, distance) in poisInRange.OrderBy(p => p.distance))
            {
                if (Current?.Poi.Id == poi.Id) continue;
                if (_queue.Any(q => q.Poi.Id == poi.Id)) continue;
                if (IsInCooldown(poi.Id)) continue;

                _queue.Add(new QueuedPoi
                {
                    Poi = poi,
                    Distance = distance,
                    Language = language,
                    EnqueuedAt = DateTime.UtcNow
                });
            }

            RaiseChanged();

            // 3. Nếu chưa có gì đang phát → phát POI đầu queue
            if (Current is null && _queue.Count > 0)
            {
                _ = PlayNextAsync();
            }
        });
    }

    public void Skip()
    {
        _ = _narration.StopAsync();
        // NarrationCompleted sẽ bắn → OnNarrationCompleted tự chuyển POI kế tiếp
    }

    public void StopAll()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _queue.Clear();
            Current = null;
            await _narration.StopAsync();
            RaiseChanged();
        });
    }

    // ====================== INTERNAL ======================

    private async Task PlayNextAsync()
    {
        if (_queue.Count == 0)
        {
            Current = null;
            return;
        }

        var next = _queue[0];
        _queue.RemoveAt(0);

        next.IsPlaying = true;
        Current = next;
        _recentlyPlayed[next.Poi.Id] = DateTime.UtcNow;

        try
        {
            await _narration.PlayAsync(new NarrationRequest
            {
                Poi = next.Poi,
                Language = next.Language,
                TriggerType = "auto_queue",
                PreferAudioFile = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioQueue] Play error: {ex.Message}");
            // Nếu phát lỗi thì chuyển POI tiếp theo luôn
            OnNarrationCompleted(this, EventArgs.Empty);
        }
    }

    private void OnNarrationCompleted(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            Current = null;
            if (_queue.Count > 0)
                await PlayNextAsync();
        });
    }

    private bool IsInCooldown(int poiId)
    {
        if (!_recentlyPlayed.TryGetValue(poiId, out var lastPlayed))
            return false;
        return DateTime.UtcNow - lastPlayed < _cooldown;
    }

    private void RaiseChanged()
        => QueueChanged?.Invoke(this, EventArgs.Empty);
}
using System.Collections.ObjectModel;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

/// <summary>
/// Hàng đợi phát audio cho nhiều POI chồng nhau.
///
/// Quy tắc:
/// - POI vào vùng → Enqueue (nếu chưa có trong queue và chưa trong cooldown)
/// - Ưu tiên: Priority cao hơn → Khoảng cách gần hơn → FIFO
/// - Worker chạy ngầm: pick POI đầu queue → phát qua NarrationService → xong thì pick tiếp
/// - POI ra khỏi vùng lúc đang phát → vẫn phát tiếp cho hết (không cắt)
/// - POI ra khỏi vùng nhưng chưa phát → remove khỏi queue
/// - POI vừa phát xong → cooldown 3 phút không phát lại
/// </summary>
public interface IAudioQueueService
{
    /// <summary>Queue hiển thị cho UI (read-only collection).</summary>
    ReadOnlyObservableCollection<QueuedPoi> Queue { get; }

    /// <summary>POI đang được phát hiện tại (null nếu không có).</summary>
    QueuedPoi? Current { get; }

    /// <summary>Sự kiện khi queue thay đổi (để UI refresh).</summary>
    event EventHandler? QueueChanged;

    /// <summary>Cập nhật danh sách POI đang ở trong vùng geofence. Tự enqueue/remove.</summary>
    void UpdateInRangePois(IEnumerable<(POI poi, double distance)> inRange, string language);

    /// <summary>Xóa toàn bộ queue (trừ POI đang phát).</summary>
    void Clear();

    /// <summary>
    /// Bỏ qua POI đang phát, tự phát POI kế tiếp.
    /// POI bị skip KHÔNG bị cooldown → có thể phát lại nếu user vào vùng lần nữa.
    /// </summary>
    void Skip();

    /// <summary>
    /// Dừng hoàn toàn việc phát: stop audio, clear queue, không phát tiếp gì cả.
    /// User phải vào vùng POI mới để queue chạy lại.
    /// </summary>
    void StopAll();
}

public class QueuedPoi
{
    public POI Poi { get; init; } = null!;
    public double Distance { get; set; }
    public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;
    public string Language { get; init; } = "vi";
    public QueueStatus Status { get; set; } = QueueStatus.Waiting;
}

public enum QueueStatus
{
    Waiting,
    Playing,
    Completed
}

public class AudioQueueService : IAudioQueueService
{
    private readonly INarrationService _narration;

    // Cooldown sau khi phát xong — 3 phút
    private const int CooldownSeconds = 180;

    // Observable collection cho UI
    private readonly ObservableCollection<QueuedPoi> _queue = new();
    public ReadOnlyObservableCollection<QueuedPoi> Queue { get; }

    // Lưu thời điểm phát xong gần nhất của từng POI (để check cooldown)
    private readonly Dictionary<int, DateTime> _lastCompletedAt = new();

    // Lock cho thao tác queue
    private readonly object _queueLock = new();

    // Worker task
    private CancellationTokenSource? _workerCts;
    private Task? _workerTask;

    // Flag: true khi user skip cái đang phát — worker sẽ không set cooldown cho POI này
    private bool _currentWasSkipped = false;

    public QueuedPoi? Current { get; private set; }
    public event EventHandler? QueueChanged;

    public AudioQueueService(INarrationService narration)
    {
        _narration = narration;
        Queue = new ReadOnlyObservableCollection<QueuedPoi>(_queue);
        StartWorker();
    }

    public void UpdateInRangePois(IEnumerable<(POI poi, double distance)> inRange, string language)
    {
        var inRangeList = inRange.ToList();
        var inRangeIds = inRangeList.Select(x => x.poi.Id).ToHashSet();

        lock (_queueLock)
        {
            var now = DateTime.UtcNow;

            // 1. Remove các POI không còn trong vùng và chưa được phát
            var toRemove = _queue
                .Where(q => q.Status == QueueStatus.Waiting && !inRangeIds.Contains(q.Poi.Id))
                .ToList();

            foreach (var item in toRemove)
                _queue.Remove(item);

            // 2. Cập nhật distance cho các POI đang trong queue
            foreach (var q in _queue.Where(x => x.Status == QueueStatus.Waiting))
            {
                var match = inRangeList.FirstOrDefault(x => x.poi.Id == q.Poi.Id);
                if (match.poi != null)
                    q.Distance = match.distance;
            }

            // 3. Enqueue POI mới
            foreach (var (poi, dist) in inRangeList)
            {
                // Đã có trong queue (Waiting hoặc Playing) → bỏ qua
                if (_queue.Any(q => q.Poi.Id == poi.Id)) continue;

                // Đang phát hiện tại → bỏ qua
                if (Current?.Poi.Id == poi.Id) continue;

                // Trong cooldown → bỏ qua
                if (_lastCompletedAt.TryGetValue(poi.Id, out var lastAt) &&
                    (now - lastAt).TotalSeconds < CooldownSeconds)
                    continue;

                _queue.Add(new QueuedPoi
                {
                    Poi = poi,
                    Distance = dist,
                    Language = language,
                    EnqueuedAt = now
                });
            }

            // 4. Sort queue: Priority ↑, Distance ↑, EnqueuedAt ↑
            ReorderQueue();
        }

        RaiseChanged();
    }

    public void Clear()
    {
        lock (_queueLock)
        {
            var toRemove = _queue.Where(q => q.Status == QueueStatus.Waiting).ToList();
            foreach (var item in toRemove)
                _queue.Remove(item);
        }
        RaiseChanged();
    }

    public void Skip()
    {
        lock (_queueLock)
        {
            // Nếu không có gì đang phát thì bỏ qua
            if (Current == null) return;
            _currentWasSkipped = true;
        }

        // Stop audio hiện tại → worker loop sẽ nhận được tín hiệu và pick POI kế tiếp
        // Tránh gọi trong lock vì StopAsync là async
        _ = _narration.StopAsync();
        System.Diagnostics.Debug.WriteLine($"[AudioQueue] Skipping current POI");
    }

    public void StopAll()
    {
        lock (_queueLock)
        {
            // Xoá tất cả các POI đang chờ
            var waiting = _queue.Where(q => q.Status == QueueStatus.Waiting).ToList();
            foreach (var item in waiting)
                _queue.Remove(item);

            // Đánh dấu current là bị skip (để không set cooldown khi stop)
            if (Current != null)
                _currentWasSkipped = true;
        }

        // Stop audio hiện tại
        _ = _narration.StopAsync();
        RaiseChanged();
        System.Diagnostics.Debug.WriteLine($"[AudioQueue] Stopped everything");
    }

    private void ReorderQueue()
    {
        // Không reorder phần đang Playing (thường là [0])
        var playing = _queue.FirstOrDefault(q => q.Status == QueueStatus.Playing);
        var waiting = _queue
            .Where(q => q.Status == QueueStatus.Waiting)
            .OrderBy(q => q.Poi.Priority)          // Priority thấp = ưu tiên cao (theo convention code cũ)
            .ThenBy(q => q.Distance)
            .ThenBy(q => q.EnqueuedAt)
            .ToList();

        _queue.Clear();
        if (playing != null) _queue.Add(playing);
        foreach (var w in waiting) _queue.Add(w);
    }

    private void StartWorker()
    {
        _workerCts = new CancellationTokenSource();
        _workerTask = Task.Run(() => WorkerLoopAsync(_workerCts.Token));
    }

    private async Task WorkerLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                QueuedPoi? next = null;

                lock (_queueLock)
                {
                    // Worker không pick khi đã có cái đang phát
                    if (Current == null)
                    {
                        next = _queue.FirstOrDefault(q => q.Status == QueueStatus.Waiting);
                        if (next != null)
                        {
                            next.Status = QueueStatus.Playing;
                            Current = next;
                        }
                    }
                }

                if (next != null)
                {
                    RaiseChanged();

                    try
                    {
                        await _narration.PlayAsync(new NarrationRequest
                        {
                            Poi = next.Poi,
                            Language = next.Language,
                            TriggerType = "queue",
                            PreferAudioFile = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioQueue] Play error: {ex.Message}");
                    }

                    // Phát xong (hoặc bị skip) — remove khỏi queue + mark cooldown NẾU không bị skip
                    lock (_queueLock)
                    {
                        if (!_currentWasSkipped)
                        {
                            // Kết thúc tự nhiên → set cooldown để tránh phát lại ngay
                            _lastCompletedAt[next.Poi.Id] = DateTime.UtcNow;
                        }
                        else
                        {
                            // User skip → KHÔNG cooldown, user có thể phát lại nếu muốn
                            System.Diagnostics.Debug.WriteLine(
                                $"[AudioQueue] {next.Poi.Name} was skipped — no cooldown applied");
                        }

                        _queue.Remove(next);
                        Current = null;
                        _currentWasSkipped = false;   // reset flag cho POI kế tiếp
                    }
                    RaiseChanged();
                }

                await Task.Delay(500, token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioQueue] Worker error: {ex.Message}");
                await Task.Delay(1000, token);
            }
        }
    }

    private void RaiseChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
            QueueChanged?.Invoke(this, EventArgs.Empty));
    }
}

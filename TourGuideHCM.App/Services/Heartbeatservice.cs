using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

/// <summary>
/// Gửi heartbeat mỗi 10 giây để backend biết thiết bị đang online.
/// Start() được gọi 1 lần khi app khởi động; Stop() chỉ gọi khi app bị destroy.
/// Timer sẽ tự pause/resume theo vòng đời OS (Android Doze, iOS background).
/// </summary>
public interface IHeartbeatService
{
    bool IsRunning { get; }
    void Start();
    void Stop();
    Task SendOnceAsync();
}

public class HeartbeatService : IHeartbeatService
{
    private readonly IApiService _api;
    private readonly IDeviceInfoService _device;
    private System.Timers.Timer? _timer;
    private const int IntervalMs = 10_000;   // 10 giây

    private readonly object _lock = new();

    public bool IsRunning
    {
        get
        {
            lock (_lock) return _timer?.Enabled == true;
        }
    }

    public HeartbeatService(IApiService api, IDeviceInfoService device)
    {
        _api = api;
        _device = device;
    }

    public void Start()
    {
        lock (_lock)
        {
            // Idempotent: nếu đã đang chạy thì bỏ qua, không tạo timer mới
            if (_timer?.Enabled == true)
            {
                Console.WriteLine("💓 Heartbeat already running — skip Start()");
                return;
            }

            // Dọn timer cũ nếu còn sót (đã stop nhưng chưa dispose)
            _timer?.Dispose();

            _timer = new System.Timers.Timer(IntervalMs);
            _timer.Elapsed += async (_, _) => await SendOnceAsync();
            _timer.AutoReset = true;
            _timer.Start();

            Console.WriteLine($"💓 Heartbeat started (every {IntervalMs / 1000}s)");
        }

        // Gửi ngay lần đầu để thiết bị xuất hiện tức thì trên Monitoring
        // (gọi ngoài lock vì là async)
        _ = SendOnceAsync();
    }

    public void Stop()
    {
        lock (_lock)
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
        Console.WriteLine("💓 Heartbeat stopped");
    }

    public async Task SendOnceAsync()
    {
        try
        {
            await _api.SendHeartbeatAsync(
                userId: _device.CurrentUserId,
                deviceId: _device.DeviceId,
                deviceName: _device.DeviceName,
                platform: _device.Platform
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💔 Heartbeat error: {ex.Message}");
        }
    }
}
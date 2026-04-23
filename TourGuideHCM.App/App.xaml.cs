using TourGuideHCM.App.Services;

namespace TourGuideHCM.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private IHeartbeatService? _heartbeat;

    public App(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;
        InitializeComponent();

        // Lấy HeartbeatService từ DI (singleton)
        _heartbeat = Services.GetService<IHeartbeatService>();

        // ====================== HEARTBEAT LIFECYCLE ======================
        // START NGAY tại constructor App — không dựa vào Window.Activated
        // (Window.Activated không fire consistently khi app resume từ background trên Android/iOS).
        //
        // Timer tự pause khi OS suspend app (Android Doze, iOS background throttling)
        // và tự resume khi app foreground lại. Đây là hành vi đúng cho mobile.
        _heartbeat?.Start();
        _ = _heartbeat?.SendOnceAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var appShell = Services.GetRequiredService<AppShell>();
        var window = new Window(appShell);

        // Safety net: khi window activated (resume từ background),
        // kiểm tra heartbeat có đang chạy không. Nếu không → restart.
        window.Activated += (_, _) =>
        {
            EnsureHeartbeatRunning("Activated");
        };

        // Resumed: MAUI event fire khi app quay từ background về foreground.
        // Đây là event đáng tin cậy nhất cho Android/iOS resume.
        window.Resumed += (_, _) =>
        {
            EnsureHeartbeatRunning("Resumed");
            // Ping ngay 1 phát để thiết bị xuất hiện tức thì trên Monitoring
            _ = _heartbeat?.SendOnceAsync();
        };

        // KHÔNG stop heartbeat khi Deactivated/Stopped (minimize).
        // Cứ để timer chạy — OS sẽ tự throttle khi app background.
        // Stop chỉ khi app bị destroy thật sự.
        window.Destroying += (_, _) =>
        {
            System.Diagnostics.Debug.WriteLine("🛑 Window destroying — stopping heartbeat");
            _heartbeat?.Stop();
        };

        return window;
    }

    private void EnsureHeartbeatRunning(string source)
    {
        if (_heartbeat == null) return;
        if (!_heartbeat.IsRunning)
        {
            System.Diagnostics.Debug.WriteLine($"💓 [{source}] Heartbeat not running — restarting");
            _heartbeat.Start();
        }
    }
}

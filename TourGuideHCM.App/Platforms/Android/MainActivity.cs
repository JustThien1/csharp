using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using TourGuideHCM.App.Services;

namespace TourGuideHCM.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "tourguide")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleIntent(intent);
    }

    private void HandleIntent(Intent? intent)
    {
        if (intent?.Action != Intent.ActionView) return;
        var uri = intent.Data;
        if (uri == null) return;

        // tourguide://poi/5 → mở POI id=5
        // tourguide://open  → mở app bình thường
        var host = uri.Host ?? "";
        var path = uri.Path ?? "";

        System.Diagnostics.Debug.WriteLine($"[DeepLink] scheme={uri.Scheme} host={host} path={path}");

        if (host == "poi" && !string.IsNullOrEmpty(path))
        {
            // path = "/5" → lấy id
            var idStr = path.TrimStart('/');
            if (int.TryParse(idStr, out var poiId))
            {
                // Gửi message tới app để navigate tới POI
                DeepLinkService.NotifyPoiRequested(poiId);
            }
        }
        // tourguide://open → không cần làm gì, app đã mở
    }
}
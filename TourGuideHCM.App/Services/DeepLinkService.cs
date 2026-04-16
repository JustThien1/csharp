namespace TourGuideHCM.App.Services;

/// <summary>
/// Bridge giữa Android Intent và MAUI app.
/// MainActivity gọi NotifyPoiRequested() → MapViewModel lắng nghe và navigate.
/// </summary>
public static class DeepLinkService
{
    public static event EventHandler<int>? PoiRequested;

    public static void NotifyPoiRequested(int poiId)
    {
        System.Diagnostics.Debug.WriteLine($"[DeepLink] POI requested: {poiId}");
        MainThread.BeginInvokeOnMainThread(() =>
            PoiRequested?.Invoke(null, poiId));
    }
}

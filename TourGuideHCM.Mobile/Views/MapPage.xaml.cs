using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Media;
using TourGuideHCM.Mobile.Models;
using TourGuideHCM.Mobile.Services;

namespace TourGuideHCM.Mobile.Views;

public partial class MapPage : ContentPage
{
    private readonly LocationService _locationService;
    private readonly POIService _poiService;

    private List<POI> _allPois = new();
    private Pin? _currentHighlight;

    public MapPage()
    {
        InitializeComponent();

        _locationService = new LocationService();
        _poiService = new POIService();

        Loaded += OnPageLoaded;
    }

    // =========================
    // LOAD PAGE
    // =========================
    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        await LoadPOIsOnMap();

        // 🔥 FIX: Không chạy geofence trên Windows để tránh crash
        try
        {
#if ANDROID || IOS
            await StartGeofenceTracking();
#endif
        }
        catch
        {
            // bỏ qua lỗi (Windows không hỗ trợ tốt)
        }
    }

    // =========================
    // LOAD POI
    // =========================
    private async Task LoadPOIsOnMap()
    {
        try
        {
            _allPois = await _poiService.GetAllAsync();

            foreach (var poi in _allPois)
            {
                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = poi.Address ?? "",
                    Location = new Location(poi.Lat, poi.Lng),
                    Type = PinType.Place
                };

                pin.InfoWindowClicked += async (s, args) =>
                {
                    await Navigation.PushAsync(new POIDetailPage(poi));
                };

                MyMap.Pins.Add(pin);
            }
        }
        catch
        {
            await DisplayAlert("Lỗi", "Không tải được POI", "OK");
        }
    }

    // =========================
    // GEOFENCE TRACKING
    // =========================
    private async Task StartGeofenceTracking()
    {
        await _locationService.StartGeofenceTrackingAsync(_allPois, poi =>
        {
            HighlightPOI(poi);
            AutoZoom(poi);
            TriggerNarration(poi);
        });
    }

    // =========================
    // HIGHLIGHT POI
    // =========================
    private void HighlightPOI(POI poi)
    {
        if (_currentHighlight != null)
            _currentHighlight.Label = _currentHighlight.Label.Replace("🔥 ", "");

        var pin = MyMap.Pins.FirstOrDefault(p => p.Label.Contains(poi.Name));

        if (pin != null)
        {
            pin.Label = "🔥 " + poi.Name;
            _currentHighlight = pin;
        }
    }

    // =========================
    // AUTO ZOOM
    // =========================
    private void AutoZoom(POI poi)
    {
        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(poi.Lat, poi.Lng),
            Distance.FromMeters(300)));
    }

    // =========================
    // TTS + AUDIO
    // =========================
    private async void TriggerNarration(POI poi)
    {
        try
        {
            if (!string.IsNullOrEmpty(poi.NarrationText))
            {
                await TextToSpeech.SpeakAsync(poi.NarrationText);
            }

            if (!string.IsNullOrEmpty(poi.AudioUrl))
            {
                await Launcher.OpenAsync(poi.AudioUrl);
            }
        }
        catch
        {
            // bỏ qua lỗi
        }
    }

    // =========================
    // SEARCH BUTTON
    // =========================
    private async void OnSearchClicked(object sender, EventArgs e)
    {
        string keyword = await DisplayPromptAsync("Tìm kiếm", "Nhập tên địa điểm:");

        if (string.IsNullOrWhiteSpace(keyword)) return;

        var result = _allPois
            .FirstOrDefault(p => p.Name.ToLower().Contains(keyword.ToLower()));

        if (result != null)
        {
            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(result.Lat, result.Lng),
                Distance.FromMeters(300)));

            HighlightPOI(result);
        }
        else
        {
            await DisplayAlert("Không tìm thấy", "Không có địa điểm phù hợp", "OK");
        }
    }

    // =========================
    // STOP TRACKING
    // =========================
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        try
        {
#if ANDROID || IOS
            _locationService.StopTracking();
#endif
        }
        catch { }
    }
}
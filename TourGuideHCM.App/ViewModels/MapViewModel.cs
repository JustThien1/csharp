using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services;
using TourGuideHCM.App.Views;

namespace TourGuideHCM.App.ViewModels;

public partial class MapViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly IGeofenceService _geofenceService;
    private readonly IApiService _apiService;
    private readonly INarrationService _narrationService;

    private List<Poi> _pois = new();
    private DateTime _lastTriggerTime = DateTime.MinValue;

    [ObservableProperty]
    private MapSpan mapSpan = MapSpan.FromCenterAndRadius(
        new Location(10.7769, 106.7009), Distance.FromKilometers(5));

    [ObservableProperty]
    private ObservableCollection<Pin> mapPins = new();

    [ObservableProperty]
    private ObservableCollection<Poi> pois = new();

    public MapViewModel(
        IDatabaseService databaseService,
        IGeofenceService geofenceService,
        IApiService apiService,
        INarrationService narrationService)
    {
        _databaseService = databaseService;
        _geofenceService = geofenceService;
        _apiService = apiService;
        _narrationService = narrationService;
    }

    // ✅ Load POI từ local DB — nhanh, không block UI
    public async Task<List<Poi>> LoadPoisLocalAsync()
    {
        try
        {
            _pois = await _databaseService.GetAllPoisAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Pois.Clear();
                foreach (var poi in _pois)
                    Pois.Add(poi);

                MapPins.Clear();
                foreach (var poi in _pois)
                {
                    var pin = new Pin
                    {
                        Label = poi.Name,
                        Address = poi.Description ?? "",
                        Location = new Location(poi.Lat, poi.Lng),
                        Type = PinType.Place
                    };

                    pin.InfoWindowClicked += async (s, e) =>
                    {
                        await Application.Current.MainPage.Navigation
                            .PushAsync(new PoiDetailPage(poi));
                    };

                    MapPins.Add(pin);
                }
            });

            Console.WriteLine($"✅ Loaded {_pois.Count} POIs từ local DB");
            return _pois;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ LoadPoisLocal: {ex.Message}");
            return new List<Poi>();
        }
    }

    // ✅ Sync từ API — chạy background, không block UI
    public async Task SyncFromApiAsync()
    {
        try
        {
            await _databaseService.SyncPoisFromApiAsync(_apiService);
            Console.WriteLine("✅ Sync API xong");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SyncFromApi: {ex.Message}");
        }
    }

    // ✅ Giữ lại cho tương thích nếu nơi khác vẫn gọi LoadPoisAsync
    [RelayCommand]
    public async Task LoadPoisAsync()
    {
        await SyncFromApiAsync();
        await LoadPoisLocalAsync();
        _ = StartTrackingAsync();
    }

    // ✅ GPS tracking chạy hoàn toàn trên background thread
    private async Task StartTrackingAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var location = await Geolocation.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium));

                    if (location != null)
                    {
                        Console.WriteLine($"📍 {location.Latitude}, {location.Longitude}");
                        CheckNearbyPOI(location.Latitude, location.Longitude);
                    }

                    await Task.Delay(5000); // ✅ 5 giây — đủ dùng, tiết kiệm pin
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GPS ERROR: " + ex.Message);
            }
        });
    }

    private void CheckNearbyPOI(double userLat, double userLng)
    {
        var nearest = _pois
            .Select(p => new { Poi = p, Distance = GetDistance(userLat, userLng, p.Lat, p.Lng) })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (nearest != null)
            HighlightPOI(nearest.Poi);

        foreach (var poi in _pois)
        {
            var distance = GetDistance(userLat, userLng, poi.Lat, poi.Lng);
            if (distance < (poi.Radius > 0 ? poi.Radius : 100))
            {
                TriggerPOI(poi);
                break;
            }
        }
    }

    private void HighlightPOI(Poi poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MapSpan = MapSpan.FromCenterAndRadius(
                new Location(poi.Lat, poi.Lng), Distance.FromMeters(300));
        });
        Console.WriteLine($"⭐ Highlight: {poi.Name}");
    }

    private async void TriggerPOI(Poi poi)
    {
        if (DateTime.Now - _lastTriggerTime < TimeSpan.FromSeconds(30)) return;

        _lastTriggerTime = DateTime.Now;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Application.Current.MainPage.DisplayAlert("📍 Gần bạn", poi.Name, "OK");
        });

        await _narrationService.PlayNarrationForPoi(poi.Id.ToString());
    }

    private double GetDistance(double lat1, double lng1, double lat2, double lng2)
    {
        var R = 6371e3;
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lng2 - lng1) * Math.PI / 180;

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                Math.Cos(φ1) * Math.Cos(φ2) *
                Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    [RelayCommand]
    public async Task GoToMyLocationAsync()
    {
        try
        {
            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium));

            if (location == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MapSpan = MapSpan.FromCenterAndRadius(
                    new Location(location.Latitude, location.Longitude),    
                    Distance.FromKilometers(2));
            });
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi GPS", ex.Message, "OK");
        }
    }
}
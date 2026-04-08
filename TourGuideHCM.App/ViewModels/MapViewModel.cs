using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Shiny.Locations;
using System.Collections.ObjectModel;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services;

namespace TourGuideHCM.App.ViewModels;

public partial class MapViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly IGeofenceService _geofenceService;
    private readonly IApiService _apiService;

    [ObservableProperty]
    private MapSpan mapSpan = MapSpan.FromCenterAndRadius(
        new Location(10.7769, 106.7009), Distance.FromKilometers(10));

    [ObservableProperty]
    private ObservableCollection<Pin> mapPins = new();

    public MapViewModel(IDatabaseService databaseService,
                        IGeofenceService geofenceService,
                        IApiService apiService)
    {
        _databaseService = databaseService;
        _geofenceService = geofenceService;
        _apiService = apiService;
    }

    [RelayCommand]
    public async Task LoadPoisAsync()
    {
        try
        {
            await _databaseService.SyncPoisFromApiAsync(_apiService);
            var pois = await _databaseService.GetAllPoisAsync();

            MapPins.Clear();

            foreach (var poi in pois)
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
                    await Application.Current.MainPage.DisplayAlert(poi.Name, poi.Description ?? "", "OK");
                };

                MapPins.Add(pin);

                // ⚠️ Tạm tắt geofence để tránh crash
                /*
                var region = new GeofenceRegion(
                    poi.Id.ToString(),
                    new Position(poi.Lat, poi.Lng),
                    Shiny.Distance.FromMeters(poi.Radius > 0 ? poi.Radius : 150))
                {
                    NotifyOnEntry = true,
                    NotifyOnExit = false
                };

                await _geofenceService.StartMonitoringAsync(region);
                */
            }

            Console.WriteLine($"✅ Đã load {pois.Count} POI.");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi Load POI", ex.Message, "OK");
        }
    }

    [RelayCommand]
    public async Task GoToMyLocationAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("Quyền", "Cần cấp quyền vị trí", "OK");
                return;
            }

            var location = await Geolocation.GetLastKnownLocationAsync()
                         ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));

            if (location == null)
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Không lấy được vị trí", "OK");
                return;
            }

            MapSpan = MapSpan.FromCenterAndRadius(
                new Location(location.Latitude, location.Longitude),
                Distance.FromKilometers(2));
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi vị trí", ex.Message, "OK");
        }
    }
}
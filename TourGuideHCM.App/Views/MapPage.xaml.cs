using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TourGuideHCM.App.ViewModels;

namespace TourGuideHCM.App.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;
    private readonly Dictionary<int, Pin> _pins = new();
    private readonly Dictionary<int, Circle> _circles = new();
    private bool _initialized;

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_initialized)
        {
            _initialized = true;
            await _vm.InitializeAsync();
            _vm.Pois.CollectionChanged += (_, _) => RefreshPins();

            var hcmCenter = new Location(10.7769, 106.7009);
            MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                hcmCenter, Distance.FromKilometers(2)));

            RefreshPins();
        }

        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MapViewModel.UserLat)
                           or nameof(MapViewModel.UserLng))
            UpdateUserLocation();

        // Khi POI gần nhất thay đổi → cập nhật highlight
        if (e.PropertyName == nameof(MapViewModel.NearestPoi))
            RefreshCircles();
    }

    private void UpdateUserLocation() { }

    private void RefreshPins()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MainMap.Pins.Clear();
            _pins.Clear();

            foreach (var poi in _vm.Pois)
            {
                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = poi.Address ?? string.Empty,
                    Location = new Location(poi.Lat, poi.Lng),
                    Type = PinType.Place
                };

                pin.MarkerClicked += (_, e) =>
                {
                    e.HideInfoWindow = false;
                    _vm.SelectPoiCommand.Execute(poi);
                };

                _pins[poi.Id] = pin;
                MainMap.Pins.Add(pin);
            }

            RefreshCircles();
        });
    }

    /// <summary>
    /// Vẽ vòng tròn bán kính cho TẤT CẢ POI.
    /// POI gần nhất → viền xanh đậm, nền xanh nhạt.
    /// POI khác     → viền xám, nền trong suốt.
    /// </summary>
    private void RefreshCircles()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Xóa toàn bộ circle cũ
            MainMap.MapElements.Clear();
            _circles.Clear();

            var nearestId = _vm.NearestPoi?.Id ?? -1;

            foreach (var poi in _vm.Pois)
            {
                bool isNearest = poi.Id == nearestId;
                double radius = poi.Radius > 0 ? poi.Radius : 100;

                var circle = new Circle
                {
                    Center = new Location(poi.Lat, poi.Lng),
                    Radius = Distance.FromMeters(radius),
                    StrokeWidth = isNearest ? 3 : 1,
                    StrokeColor = isNearest
                        ? Color.FromArgb("#FF1976D2")   // xanh đậm
                        : Color.FromArgb("#88888888"),  // xám nhạt
                    FillColor = isNearest
                        ? Color.FromArgb("#221976D2")   // xanh rất nhạt
                        : Color.FromArgb("#11888888"),  // xám rất nhạt
                };

                _circles[poi.Id] = circle;
                MainMap.MapElements.Add(circle);
            }
        });
    }

    private void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        _vm.SelectedPoi = null;
    }
    private async void OnMyLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync()
                        ?? await Geolocation.GetLocationAsync(new GeolocationRequest
                        {
                            DesiredAccuracy = GeolocationAccuracy.Medium,
                            Timeout = TimeSpan.FromSeconds(5)
                        });

            if (location is not null)
            {
                MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(location.Latitude, location.Longitude),
                    Distance.FromKilometers(1)));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Location] {ex.Message}");
        }
    }
}
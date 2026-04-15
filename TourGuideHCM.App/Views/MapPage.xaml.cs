using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.ViewModels;

namespace TourGuideHCM.App.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;
    private readonly Dictionary<int, Pin> _pins = new();

    public MapPage(MapViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _vm.InitializeAsync();

        _vm.Pois.CollectionChanged += (_, _) => RefreshPins();
        _vm.PropertyChanged += OnViewModelPropertyChanged;

        // Đặt camera về trung tâm HCM
        var hcmCenter = new Location(10.7769, 106.7009);
        MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            hcmCenter, Distance.FromKilometers(2)));

        RefreshPins();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        // UserLat / UserLng (đúng tên ViewModel)
        if (e.PropertyName is nameof(MapViewModel.UserLat)
                           or nameof(MapViewModel.UserLng))
        {
            UpdateUserLocation();
        }
    }

    private void UpdateUserLocation()
    {
        // Map tự hiển thị vị trí user qua IsShowingUser="True"
        // Không cần di chuyển camera mỗi lần update
    }

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
                    Address = poi.Address ?? poi.ShortDescription,
                    // Dùng poi.Lat / poi.Lng (đúng tên Model)
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
        });
    }

    private void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        _vm.SelectedPoi = null;
    }
}

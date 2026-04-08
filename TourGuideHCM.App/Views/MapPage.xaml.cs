using System.Text.Json;
using System.IO;
using TourGuideHCM.App.ViewModels;

namespace TourGuideHCM.App.Views;

public partial class MapPage : ContentPage
{
    private MapViewModel _viewModel;

    public MapPage()
    {
        InitializeComponent();

        _viewModel = MauiProgram.Services.GetService<MapViewModel>();

        if (_viewModel == null)
            throw new Exception("Không lấy được MapViewModel");

        BindingContext = _viewModel;

        // 🔥 QUAN TRỌNG: bắt event khi WebView load xong
        MyWebView.Navigated += OnWebViewLoaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMap();
    }

    // 🔥 CHỈ LOAD HTML (KHÔNG GỌI JS Ở ĐÂY)
    private async Task LoadMap()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            using var reader = new StreamReader(stream);

            var html = await reader.ReadToEndAsync();

            MyWebView.Source = new HtmlWebViewSource
            {
                Html = html
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Map", ex.Message, "OK");
        }
    }

    // 🔥 QUAN TRỌNG NHẤT: chạy khi WebView load xong
    private async void OnWebViewLoaded(object sender, WebNavigatedEventArgs e)
    {
        try
        {
            await _viewModel.LoadPoisAsync();

            var pois = _viewModel.MapPins.Select(p => new
            {
                name = p.Label,
                description = p.Address,
                lat = p.Location.Latitude,
                lng = p.Location.Longitude
            });

            var json = JsonSerializer.Serialize(pois);

            // 👉 Gửi dữ liệu sang JS
            await MyWebView.EvaluateJavaScriptAsync($"loadFromApp({json})");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi JS", ex.Message, "OK");
        }
    }

    // 📍 Nút vị trí hiện tại
    private async void OnGoToLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync()
                         ?? await Geolocation.GetLocationAsync();

            if (location == null)
            {
                await DisplayAlert("Lỗi", "Không lấy được vị trí", "OK");
                return;
            }

            await MyWebView.EvaluateJavaScriptAsync(
                $"goToLocation({location.Latitude}, {location.Longitude})");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi GPS", ex.Message, "OK");
        }
    }

    // 🔄 Reload
    private async void OnReloadClicked(object sender, EventArgs e)
    {
        await LoadMap();
    }
}
using System.Text.Json;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.ViewModels;

namespace TourGuideHCM.App.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private List<Poi> _allPois = new();
    private bool _mapLoaded = false; // ✅ Tránh load lại nhiều lần

    public MapPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MapViewModel>();
        BindingContext = _viewModel;

#if ANDROID
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping(nameof(WebView), (handler, view) =>
        {
            if (handler.PlatformView is Android.Webkit.WebView webView)
            {
                webView.Settings.JavaScriptEnabled = true;
                webView.Settings.DomStorageEnabled = true;
                webView.Settings.AllowFileAccess = true;
                webView.Settings.AllowFileAccessFromFileURLs = true;
                webView.Settings.AllowUniversalAccessFromFileURLs = true;
                webView.Settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
                webView.Settings.LoadsImagesAutomatically = true;
            }
        });
#endif

        MyWebView.Navigated += OnWebViewLoaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ✅ Chỉ load map 1 lần duy nhất
        if (!_mapLoaded)
        {
            await LoadMap();
            _mapLoaded = true;
        }
    }

    private async Task LoadMap()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            var html = await new StreamReader(stream).ReadToEndAsync();

            MyWebView.Source = new HtmlWebViewSource
            {
                Html = html,
                BaseUrl = "file:///android_asset/"
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Load Map", ex.Message, "OK");
        }
    }

    private async void OnWebViewLoaded(object? sender, WebNavigatedEventArgs e)
    {
        try
        {
            // ✅ Bỏ Task.Delay(3000) — chờ thực tế thay vì cứng
            await Task.Delay(500);

            // ✅ Load POI từ DB local trước (nhanh), sync API ở background
            _allPois = await _viewModel.LoadPoisLocalAsync();

            // ✅ Sync API ở background, không block UI
            _ = Task.Run(async () =>
            {
                await _viewModel.SyncFromApiAsync();

                // Reload lại POI sau khi sync xong
                _allPois = await _viewModel.LoadPoisLocalAsync();
                await PushPoisToMapAsync();
            });

            await PushPoisToMapAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ OnWebViewLoaded: {ex.Message}");
        }
    }

    private async Task PushPoisToMapAsync()
    {
        var poisForJs = _allPois.Select(p => new
        {
            id = p.Id,
            name = p.Name ?? "",
            description = p.Description ?? "",
            lat = p.Lat,
            lng = p.Lng
        }).ToList();

        var json = JsonSerializer.Serialize(poisForJs);

        // ✅ Đảm bảo chạy trên main thread
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await MyWebView.EvaluateJavaScriptAsync($"loadFromApp({json})");
        });

        Console.WriteLine($"✅ Pushed {_allPois.Count} POIs to map");
    }

    public async Task GoToPoiAsync(double lat, double lng, string name)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await MyWebView.EvaluateJavaScriptAsync(
                $"goToPoi({lat}, {lng}, \"{name.Replace("\"", "\\\"")}\")");
        });
    }

    private void OnHamburgerClicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async void OnGoToLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Quyền", "Cần cấp quyền vị trí", "OK");
                return;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
            var location = await Geolocation.GetLocationAsync(request) ?? new Location(10.7769, 106.7009);

            await MyWebView.EvaluateJavaScriptAsync(
                $"goToLocation({location.Latitude}, {location.Longitude})");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi GPS", ex.Message, "OK");
        }
    }

    private async void OnReloadClicked(object sender, EventArgs e)
    {
        _mapLoaded = false;
        MyWebView.Source = null;
        await Task.Delay(300);
        await LoadMap();
    }
}
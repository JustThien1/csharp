using System.Text.Json;
using TourGuideHCM.App.ViewModels;

namespace TourGuideHCM.App.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;

    public MapPage()
    {
        InitializeComponent();

        _viewModel = MauiProgram.Services.GetRequiredService<MapViewModel>();
        BindingContext = _viewModel;

        // Cấu hình WebView cho Android
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
        await LoadMap();
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

            Console.WriteLine("✅ map.html loaded into WebView");
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
            Console.WriteLine($"🌐 WebView Navigated: {e.Url} - Result: {e.Result}");

            // Chờ Leaflet khởi tạo
            await Task.Delay(2500);

            await _viewModel.LoadPoisAsync();

            var pois = _viewModel.MapPins.Select(p => new
            {
                name = p.Label ?? "",
                description = p.Address ?? "",
                lat = p.Location.Latitude,
                lng = p.Location.Longitude
            }).ToList();

            var json = JsonSerializer.Serialize(pois);

            Console.WriteLine($"📤 Gửi JSON: {json}");

            // Gọi JS an toàn trên main thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await MyWebView.EvaluateJavaScriptAsync($"loadFromApp({json})");
            });

            Console.WriteLine("✅ Đã gọi loadFromApp()");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi OnWebViewLoaded: {ex.Message}");
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private async void OnGoToLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync()
                         ?? await Geolocation.GetLocationAsync();

            if (location != null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MyWebView.EvaluateJavaScriptAsync($"goToLocation({location.Latitude}, {location.Longitude})");
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi GPS", ex.Message, "OK");
        }
    }

    private async void OnReloadClicked(object sender, EventArgs e)
    {
        await LoadMap();
    }
}
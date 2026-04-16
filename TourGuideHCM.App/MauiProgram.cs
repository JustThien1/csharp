using CommunityToolkit.Maui;
using Plugin.Maui.Audio;
using TourGuideHCM.App.Services;
using TourGuideHCM.App.Services.Interfaces;
using TourGuideHCM.App.ViewModels;
using TourGuideHCM.App.Views;

namespace TourGuideHCM.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── HttpClient ────────────────────────────────────────────────────────
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5284/"),
            Timeout = TimeSpan.FromSeconds(30)
        });

        // IAudioManager
        builder.Services.AddSingleton<IAudioManager>(
            Plugin.Maui.Audio.AudioManager.Current);

        // ── Services ──────────────────────────────────────────────────────────
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
        builder.Services.AddSingleton<INarrationService, NarrationService>();

        // ── ViewModels ────────────────────────────────────────────────────────
        builder.Services.AddSingleton<MapViewModel>();
        builder.Services.AddTransient<PoiViewModel>();
        builder.Services.AddTransient<AuthViewModel>();

        // ── Pages ─────────────────────────────────────────────────────────────
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<MapPage>();
        builder.Services.AddTransient<PoiListPage>();
        builder.Services.AddTransient<PoiDetailPage>();
        builder.Services.AddTransient<PoiBottomSheet>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();

        return builder.Build();
    }
}
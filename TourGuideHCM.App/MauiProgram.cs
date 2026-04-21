using CommunityToolkit.Maui;
using Plugin.Maui.Audio;
using TourGuideHCM.App.Helpers;
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

        builder.Services.AddSingleton<IAudioManager>(Plugin.Maui.Audio.AudioManager.Current);

        builder.Services.AddHttpClient("DefaultClient", client =>
        {
            client.BaseAddress = new Uri(DeviceHelper.GetBaseUrl());
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ─── Services ────────────────────────────────────────────────────────
        builder.Services.AddSingleton<IDeviceInfoService, DeviceInfoService>(); // ⭐ FIX DI
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
        builder.Services.AddSingleton<INarrationService, NarrationService>();
        builder.Services.AddSingleton<IAudioQueueService, AudioQueueService>();

        // ─── ViewModels ──────────────────────────────────────────────────────
        builder.Services.AddSingleton<MapViewModel>();
        builder.Services.AddTransient<PoiViewModel>();
        builder.Services.AddTransient<HomeViewModel>();

        // ─── Pages ───────────────────────────────────────────────────────────
        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddSingleton<MapPage>();
        builder.Services.AddTransient<PoiListPage>();
        builder.Services.AddTransient<PoiDetailPage>();
        builder.Services.AddTransient<PoiBottomSheet>();

        return builder.Build();
    }
}
using CommunityToolkit.Maui;
using Shiny;
using TourGuideHCM.App.Services;
using TourGuideHCM.App.ViewModels;
using TourGuideHCM.App.Views;

namespace TourGuideHCM.App;

public static class MauiProgram
{
    // ✅ THÊM DÒNG NÀY
    public static IServiceProvider Services { get; private set; }

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseShiny();

        // Services
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<INarrationService, NarrationService>();
        builder.Services.AddSingleton<IGeofenceService, GeofenceService>();

        builder.Services.AddGeofencing<GeofenceDelegate>();

        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<MapPage>();

        var app = builder.Build();

        // ✅ QUAN TRỌNG
        Services = app.Services;

        return app;
    }
}
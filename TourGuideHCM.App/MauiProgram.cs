using CommunityToolkit.Maui;
using Shiny;
using TourGuideHCM.App.Services;
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
            .UseMauiCommunityToolkit()
            .UseShiny();

        // Services
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<INarrationService, NarrationService>();
        builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
        builder.Services.AddSingleton<AuthService>();

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5284")
        });
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();

        builder.Services.AddGeofencing<GeofenceDelegate>();

        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<MapPage>();

        return builder.Build();
    }
}
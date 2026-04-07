using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Maps; // thêm dòng này
using TourGuideHCM.Mobile.Services;
using TourGuideHCM.Mobile.Views.Auth;

namespace TourGuideHCM.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps() // 🔥 THÊM DÒNG NÀY
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<POIService>();
        builder.Services.AddSingleton<LocationService>();

        return builder.Build();
    }
}
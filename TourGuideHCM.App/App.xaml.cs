using TourGuideHCM.App.Views;

namespace TourGuideHCM.App
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public App(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var username = Preferences.Get("username", null);

            if (!string.IsNullOrEmpty(username))
            {
                // Đã login → tạo AppShell với dependency injection đúng cách
                var appShell = Services.GetRequiredService<AppShell>();
                return new Window(appShell);
            }

            // Chưa login → vào LoginPage
            var loginPage = Services.GetService<LoginPage>();
            return new Window(new NavigationPage(loginPage));
        }
    }
}
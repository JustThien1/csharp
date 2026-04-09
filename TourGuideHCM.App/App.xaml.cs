using TourGuideHCM.App.Views;

namespace TourGuideHCM.App
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } // 🔥 PHẢI CÓ

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            Services = serviceProvider; // 🔥 PHẢI CÓ
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var username = Preferences.Get("username", null);

            if (!string.IsNullOrEmpty(username))
            {
                // 👉 đã login rồi → vào thẳng app
                return new Window(new AppShell());
            }

            // 👉 chưa login → hiện login
            var loginPage = Services.GetService<LoginPage>();
            return new Window(new NavigationPage(loginPage));
        }
    }
}
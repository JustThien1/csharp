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
            var loginPage = Services.GetService<LoginPage>();
            return new Window(new NavigationPage(loginPage));
        }
    }
}
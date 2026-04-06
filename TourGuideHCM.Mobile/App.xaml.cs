using TourGuideHCM.Mobile.Views.Auth;

namespace TourGuideHCM.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // 👉 CÁI QUAN TRỌNG BẠN ĐANG THIẾU
            return new Window(new NavigationPage(new LoginPage()));
        }
    }
}
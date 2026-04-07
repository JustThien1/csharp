using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using TourGuideHCM.Mobile.Views.Auth;

namespace TourGuideHCM.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        bool isLoggedIn = Preferences.Get("isLoggedIn", false);

        if (isLoggedIn)
        {
            MainPage = new AppShell();
        }
        else
        {
            MainPage = new NavigationPage(new LoginPage());
        }
    }
}
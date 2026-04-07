using TourGuideHCM.Mobile.Views.Auth;

namespace TourGuideHCM.Mobile.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        LoadUserInfo();
    }

    private void LoadUserInfo()
    {
        string username = Preferences.Get("username", "User");
        lblWelcome.Text = $"Xin chào, {username} 👋";
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Remove("isLoggedIn");
        Preferences.Remove("username");

        // Quay về màn hình Login
        Application.Current!.MainPage = new NavigationPage(new LoginPage());
    }
    private async void OnGoToMapClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage());
    }
}
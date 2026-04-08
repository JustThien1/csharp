namespace TourGuideHCM.Mobile.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        Application.Current!.MainPage = new NavigationPage(new LoginPage());
    }
}
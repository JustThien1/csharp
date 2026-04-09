using TourGuideHCM.App.Services;

namespace TourGuideHCM.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;

    // ✅ DÙNG DI CHUẨN
    public LoginPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            if (_auth == null)
            {
                await DisplayAlert("ERROR", "AuthService NULL", "OK");
                return;
            }

            var ok = await _auth.Login(
                UsernameEntry.Text,
                PasswordEntry.Text);

            if (ok)
            {
                Preferences.Set("username", UsernameEntry.Text);
                await DisplayAlert("OK", "Login success", "OK");
                Application.Current.MainPage = new AppShell();
            }
            else
            {
                await DisplayAlert("FAIL", "Login failed", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", ex.Message, "OK");
        }
    }

    private async void OnGoRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_auth));
    }
}
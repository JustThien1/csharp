using TourGuideHCM.App.Services;

namespace TourGuideHCM.App.Views;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _auth;

    public RegisterPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        try
        {
            var ok = await _auth.Register(
                UsernameEntry.Text,
                PasswordEntry.Text);

            if (ok)
            {
                await DisplayAlert("OK", "Register success", "OK");
                await Navigation.PopAsync(); // quay lại login
            }
            else
            {
                await DisplayAlert("FAIL", "Register failed", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", ex.Message, "OK");
        }
    }
}
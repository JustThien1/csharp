using Microsoft.Maui.Storage;
using TourGuideHCM.App.Services;

namespace TourGuideHCM.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;

    public LoginPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            var userId = await _auth.Login(
                UsernameEntry.Text?.Trim() ?? "",
                PasswordEntry.Text?.Trim() ?? "");

            if (userId.HasValue && userId.Value > 0)
            {
                // Lưu userId để PlaybackService sử dụng
                Preferences.Set("userId", userId.Value);
                Preferences.Set("username", $"User_{userId.Value}");

                await DisplayAlert("Thành công", "Đăng nhập thành công!", "OK");

                Application.Current.MainPage = App.Services.GetRequiredService<AppShell>();
            }
            else
            {
                await DisplayAlert("Thất bại", "Tên đăng nhập hoặc mật khẩu không đúng.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private async void OnGoRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_auth));
    }
}
using TourGuideHCM.Mobile.Services;
using TourGuideHCM.Mobile.Views;

namespace TourGuideHCM.Mobile.Views.Auth;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;

    public LoginPage()
    {
        InitializeComponent();
        _authService = new AuthService();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            string username = txtUsername.Text?.Trim() ?? "";
            string password = txtPassword.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ thông tin", "OK");
                return;
            }

            bool success = await _authService.LoginAsync(username, password);

            if (success)
            {
                // Lưu thông tin đăng nhập
                Preferences.Set("isLoggedIn", true);
                Preferences.Set("username", username);

                await DisplayAlert("Thành công", "Đăng nhập thành công!", "OK");

                // 🔥 CHUYỂN SANG AppShell (không dùng PushAsync nữa)
                // Điều này sẽ thay thế toàn bộ giao diện hiện tại bằng Shell có Tab
                Application.Current!.MainPage = new AppShell();
            }
            else
            {
                await DisplayAlert("Lỗi", "Sai tài khoản hoặc mật khẩu", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi hệ thống", $"Đã xảy ra lỗi: {ex.Message}", "OK");
        }
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        // Giữ nguyên chức năng đăng ký
        await Navigation.PushAsync(new RegisterPage());
    }
}
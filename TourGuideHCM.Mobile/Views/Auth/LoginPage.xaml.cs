using TourGuideHCM.Mobile.Services;
using TourGuideHCM.Mobile.Views; // HomePage nằm ngoài Auth

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

            // Validate
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ thông tin", "OK");
                return;
            }

            // Gọi service
            bool success = await _authService.LoginAsync(username, password);

            if (success)
            {
                await DisplayAlert("Thành công", "Đăng nhập thành công!", "OK");

                // 👉 Debug checkpoint
                Console.WriteLine("Navigate to HomePage");

                // Chuyển trang
                await Navigation.PushAsync(new HomePage());
            }
            else
            {
                await DisplayAlert("Lỗi", "Sai tài khoản hoặc mật khẩu", "OK");
            }
        }
        catch (Exception ex)
        {
            // 👉 Bắt lỗi để không crash nữa
            await DisplayAlert("Lỗi hệ thống", ex.Message, "OK");
        }
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}
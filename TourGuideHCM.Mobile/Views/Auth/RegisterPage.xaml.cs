using TourGuideHCM.Mobile.Services;

namespace TourGuideHCM.Mobile.Views.Auth;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService;

    public RegisterPage()
    {
        InitializeComponent();
        _authService = new AuthService();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string username = txtUsername.Text?.Trim();
        string password = txtPassword.Text?.Trim();
        string fullName = txtFullName.Text?.Trim();
        string email = txtEmail.Text?.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Lỗi", "Tên đăng nhập và mật khẩu không được để trống", "OK");
            return;
        }

        bool success = await _authService.RegisterAsync(username, password, fullName, email);

        if (success)
        {
            await DisplayAlert("Thành công", "Đăng ký tài khoản thành công!\nBạn có thể đăng nhập ngay bây giờ.", "OK");
            await Navigation.PopAsync(); // Quay về trang Login
        }
        else
        {
            await DisplayAlert("Thất bại", "Đăng ký không thành công.\nUsername có thể đã tồn tại.", "OK");
        }
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
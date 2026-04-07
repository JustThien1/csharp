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
        string username = txtUsername.Text?.Trim() ?? "";
        string password = txtPassword.Text?.Trim() ?? "";
        string fullName = txtFullName.Text?.Trim() ?? "";
        string email = txtEmail.Text?.Trim() ?? "";

        // Validate
        if (string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(fullName) ||
            string.IsNullOrEmpty(email))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ thông tin", "OK");
            return;
        }

        bool success = await _authService.RegisterAsync(username, password, fullName, email);

        if (success)
        {
            await DisplayAlert("Thành công", "Đăng ký thành công!", "OK");

            // 👉 quay lại Login
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Lỗi", "Đăng ký thất bại", "OK");
        }
    }
}
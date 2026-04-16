using TourGuideHCM.App.Services;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Views;

public partial class RegisterPage : ContentPage
{
    private readonly IAuthService _auth;

    public RegisterPage(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";
        var fullName = FullNameEntry.Text?.Trim() ?? "";
        var phone = PhoneEntry.Text?.Trim() ?? "";
        var email = EmailEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng nhập tên đăng nhập và mật khẩu");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải có ít nhất 6 ký tự");
            return;
        }

        SetLoading(true);
        try
        {
            bool ok;

            // Dùng AuthService cast để gọi overload có phone
            if (_auth is AuthService authService)
            {
                ok = await authService.RegisterWithPhoneAsync(username, password, fullName, email, phone);
            }
            else
            {
                // Fallback: lưu phone vào Preferences trước khi gọi interface
                Preferences.Set("pending_phone", phone);
                ok = await _auth.RegisterAsync(username, password, fullName, email);
            }

            if (ok)
            {
                await DisplayAlert("✅ Thành công", "Tài khoản đã được tạo!", "Đăng nhập ngay");
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                ShowError("Tên đăng nhập đã tồn tại hoặc có lỗi xảy ra");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi kết nối: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//LoginPage");

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }

    private void SetLoading(bool v)
    {
        LoadingIndicator.IsRunning = v;
        LoadingIndicator.IsVisible = v;
        RegisterBtn.IsEnabled = !v;
        ErrorLabel.IsVisible = false;
    }
}

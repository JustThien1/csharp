using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Views;

public partial class RegisterPage : ContentPage
{
    private readonly IAuthService _auth;

    public RegisterPage(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
        RegisterBtn.Clicked += OnRegisterClicked;
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim();
        var fullName = FullNameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text;
        var confirm = ConfirmEntry.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName)
            || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng điền đầy đủ thông tin bắt buộc (*).");
            return;
        }
        if (password.Length < 6) { ShowError("Mật khẩu phải có ít nhất 6 ký tự."); return; }
        if (password != confirm) { ShowError("Mật khẩu xác nhận không khớp."); return; }

        SetLoading(true);
        try
        {
            var ok = await _auth.RegisterAsync(username, password, fullName, email);
            if (ok)
            {
                Preferences.Set("username", username);
                Preferences.Set("userId", _auth.CurrentUser?.Id ?? 0);
                await DisplayAlert("Thành công", "Tài khoản đã được tạo!", "OK");
                await Shell.Current.GoToAsync("//MapPage");
            }
            else ShowError("Tên đăng nhập đã tồn tại hoặc có lỗi xảy ra.");
        }
        catch (Exception ex) { ShowError($"Lỗi kết nối: {ex.Message}"); }
        finally { SetLoading(false); }
    }

    private void ShowError(string msg) { ErrorLabel.Text = msg; ErrorLabel.IsVisible = true; }

    private void SetLoading(bool v)
    {
        LoadingIndicator.IsRunning = v;
        LoadingIndicator.IsVisible = v;
        RegisterBtn.IsEnabled = !v;
        ErrorLabel.IsVisible = false;
    }
}

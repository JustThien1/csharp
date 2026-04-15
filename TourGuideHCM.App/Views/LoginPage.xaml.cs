using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _auth;

    public LoginPage(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
        LoginBtn.Clicked += async (_, _) => await DoLoginAsync();
        GuestBtn.Clicked += async (_, _) => await GoToMainAsync();
        RegisterBtn.Clicked += async (_, _) =>
            await Shell.Current.GoToAsync("//RegisterPage");
        PasswordEntry.Completed += async (_, _) => await DoLoginAsync();
    }

    private async Task DoLoginAsync()
    {
        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng nhập tên đăng nhập và mật khẩu.");
            return;
        }

        SetLoading(true);
        try
        {
            var ok = await _auth.LoginAsync(username, password);
            if (ok)
            {
                // Lưu username để AppShell biết đã login
                Preferences.Set("username", username);
                Preferences.Set("userId", _auth.CurrentUser?.Id ?? 0);
                await GoToMainAsync();
            }
            else
            {
                ShowError("Tên đăng nhập hoặc mật khẩu không đúng.");
            }
        }
        catch (Exception ex) { ShowError($"Lỗi kết nối: {ex.Message}"); }
        finally { SetLoading(false); }
    }

    private static async Task GoToMainAsync()
    {
        // Navigate về MapPage (FlyoutItem đầu tiên trong AppShell)
        await Shell.Current.GoToAsync("//MapPage");
    }

    private void ShowError(string msg) { ErrorLabel.Text = msg; ErrorLabel.IsVisible = true; }

    private void SetLoading(bool v)
    {
        LoadingIndicator.IsRunning = v;
        LoadingIndicator.IsVisible = v;
        LoginBtn.IsEnabled = !v;
        GuestBtn.IsEnabled = !v;
        ErrorLabel.IsVisible = false;
    }
}

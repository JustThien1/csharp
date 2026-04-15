using TourGuideHCM.App.Services;
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
        RegisterBtn.Clicked += async (_, _) => await Shell.Current.GoToAsync("//RegisterPage");
        PasswordEntry.Completed += async (_, _) => await DoLoginAsync();

        LanguageService.LanguageChanged += (_, _) => RefreshText();
        RefreshText();
    }

    private void OnLangClicked(object sender, EventArgs e)
    {
        LanguageService.Instance.Toggle();
    }

    private void RefreshText()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TitleLabel.Text = AppLanguage.LoginTitle;
            UsernameEntry.Placeholder = AppLanguage.Username;
            PasswordEntry.Placeholder = AppLanguage.Password;
            LoginBtn.Text = AppLanguage.LoginBtn;
            GuestBtn.Text = AppLanguage.GuestBtn;
            RegisterBtn.Text = AppLanguage.RegisterBtn;
            LangBtn.Text = LanguageService.Instance.ToggleLabel;
        });
    }

    private async Task DoLoginAsync()
    {
        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        { ShowError(AppLanguage.LoginRequired); return; }

        SetLoading(true);
        try
        {
            var ok = await _auth.LoginAsync(username, password);
            if (ok)
            {
                Preferences.Set("username", username);
                Preferences.Set("userId", _auth.CurrentUser?.Id ?? 0);
                await GoToMainAsync();
            }
            else ShowError(AppLanguage.LoginError);
        }
        catch (Exception ex) { ShowError($"{AppLanguage.ConnectError}: {ex.Message}"); }
        finally { SetLoading(false); }
    }

    private static async Task GoToMainAsync()
        => await Shell.Current.GoToAsync("//MapPage");

    private void ShowError(string msg)
    { ErrorLabel.Text = msg; ErrorLabel.IsVisible = true; }

    private void SetLoading(bool v)
    {
        LoadingIndicator.IsRunning = v;
        LoadingIndicator.IsVisible = v;
        LoginBtn.IsEnabled = !v;
        GuestBtn.IsEnabled = !v;
        ErrorLabel.IsVisible = false;
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.ViewModels;

public class AuthViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _auth;

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    private string _fullName = string.Empty;
    public string FullName
    {
        get => _fullName;
        set { _fullName = value; OnPropertyChanged(); }
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public event EventHandler? LoginSucceeded;

    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand GuestCommand { get; }

    public AuthViewModel(IAuthService auth)
    {
        _auth = auth;
        LoginCommand = new Command(async () => await DoLoginAsync(), () => !IsBusy);
        RegisterCommand = new Command(async () => await DoRegisterAsync(), () => !IsBusy);
        GuestCommand = new Command(() => LoginSucceeded?.Invoke(this, EventArgs.Empty));
    }

    private async Task DoLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu.";
            return;
        }
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var ok = await _auth.LoginAsync(Username.Trim(), Password);
            if (ok) LoginSucceeded?.Invoke(this, EventArgs.Empty);
            else ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
        }
        catch { ErrorMessage = "Lỗi kết nối. Vui lòng thử lại."; }
        finally { IsBusy = false; }
    }

    private async Task DoRegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng điền đầy đủ thông tin.";
            return;
        }
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var ok = await _auth.RegisterAsync(Username.Trim(), Password,
                FullName.Trim(), Email.Trim());
            if (ok) LoginSucceeded?.Invoke(this, EventArgs.Empty);
            else ErrorMessage = "Tên đăng nhập đã tồn tại.";
        }
        catch { ErrorMessage = "Lỗi kết nối. Vui lòng thử lại."; }
        finally { IsBusy = false; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

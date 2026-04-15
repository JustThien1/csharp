using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TourGuideHCM.App.Services;

/// <summary>
/// Quản lý ngôn ngữ app — VI hoặc EN.
/// Lưu lựa chọn vào Preferences để nhớ sau khi restart.
/// </summary>
public class LanguageService : INotifyPropertyChanged
{
    private static LanguageService? _instance;
    public static LanguageService Instance => _instance ??= new LanguageService();

    private bool _isEnglish;

    /// <summary>true = English, false = Tiếng Việt</summary>
    public static bool IsEnglish => Instance._isEnglish;

    public string CurrentLanguage => _isEnglish ? "EN" : "VI";
    public string ToggleLabel => _isEnglish ? "🇻🇳 Tiếng Việt" : "🇬🇧 English";

    private LanguageService()
    {
        // Đọc lựa chọn đã lưu
        _isEnglish = Preferences.Get("app_language_en", false);
    }

    /// <summary>Đổi sang ngôn ngữ cụ thể</summary>
    public void SetEnglish(bool english)
    {
        if (_isEnglish == english) return;
        _isEnglish = english;
        Preferences.Set("app_language_en", english);
        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged(nameof(ToggleLabel));
        // Notify toàn app
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Toggle EN ↔ VI</summary>
    public void Toggle() => SetEnglish(!_isEnglish);

    /// <summary>Fire khi ngôn ngữ thay đổi — các ViewModel subscribe để refresh UI</summary>
    public static event EventHandler? LanguageChanged;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

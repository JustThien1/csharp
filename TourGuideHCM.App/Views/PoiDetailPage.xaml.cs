using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Views;

[QueryProperty(nameof(Poi), "Poi")]
public partial class PoiDetailPage : ContentPage
{
    private readonly INarrationService _narration;
    // KHÔNG CÒN SignalRService

    private POI? _poi;
    public POI? Poi
    {
        get => _poi;
        set { _poi = value; BindingContext = value; }
    }

    public PoiDetailPage(INarrationService narration)   // ← Bỏ SignalRService
    {
        InitializeComponent();
        _narration = narration;

        PlayViBtn.Clicked += async (_, _) => await PlayAsync("vi");
        PlayEnBtn.Clicked += async (_, _) => await PlayAsync("en");

        StopBtn.Clicked += async (_, _) =>
        {
            await _narration.StopAsync();
            UpdateButtons(false);
        };

        _narration.NarrationStarted += (_, _) => MainThread.BeginInvokeOnMainThread(() => UpdateButtons(true));
        _narration.NarrationCompleted += (_, _) => MainThread.BeginInvokeOnMainThread(() => UpdateButtons(false));

        LanguageService.LanguageChanged += (_, _) => RefreshText();
        RefreshText();
    }

    private async Task PlayAsync(string language)
    {
        if (_poi is null) return;

        await _narration.PlayAsync(new NarrationRequest
        {
            Poi = _poi,
            Language = language,
            TriggerType = "manual",
            PreferAudioFile = true
        });
    }

    private void UpdateButtons(bool isPlaying)
    {
        PlayViBtn.IsEnabled = !isPlaying;
        PlayEnBtn.IsEnabled = !isPlaying;
        StopBtn.IsVisible = isPlaying;
    }

    private void RefreshText()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlayViBtn.Text = "▶  Tiếng Việt";
            PlayEnBtn.Text = "▶  English";
            StopBtn.Text = LanguageService.IsEnglish ? "⏹  Stop Narration" : "⏹  Dừng thuyết minh";
            IntroLabel.Text = LanguageService.IsEnglish ? "Introduction" : "Giới thiệu";
            LocationInfoLabel.Text = LanguageService.IsEnglish ? "Location Info" : "Thông tin vị trí";
            LatLabel.Text = LanguageService.IsEnglish ? "Latitude" : "Vĩ độ";
            LngLabel.Text = LanguageService.IsEnglish ? "Longitude" : "Kinh độ";
        });
    }
}
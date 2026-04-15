using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Views;

[QueryProperty(nameof(Poi), "Poi")]
public partial class PoiDetailPage : ContentPage
{
    private readonly INarrationService _narration;

    private POI? _poi;
    public POI? Poi
    {
        get => _poi;
        set
        {
            _poi = value;
            BindingContext = value;
        }
    }

    public PoiDetailPage(INarrationService narration)
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

        _narration.NarrationStarted += (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => UpdateButtons(true));
        _narration.NarrationCompleted += (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => UpdateButtons(false));
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
}

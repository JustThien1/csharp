using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services;

namespace TourGuideHCM.App.Views;

public partial class PoiBottomSheet : ContentPage
{
    private readonly INarrationService _narrationService;
    private readonly Poi _poi;

    public PoiBottomSheet(Poi poi)
    {
        InitializeComponent();
        BindingContext = _poi = poi;

        _narrationService = App.Services.GetService<INarrationService>();
    }

    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        if (_narrationService != null)
        {
            await _narrationService.PlayNarrationForPoi(_poi.Id.ToString());

            var playback = App.Services.GetService<PlaybackService>();
            var userId = Preferences.Get("userId", 0);

            await playback.LogPlayback(userId, _poi.Id);
        }
    }
}
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services;

namespace TourGuideHCM.App.Views;

public partial class PoiDetailPage : ContentPage
{
    private readonly INarrationService _narrationService;
    private Poi _poi;

    public PoiDetailPage(Poi poi)
    {
        InitializeComponent();
        BindingContext = poi;
        _poi = poi;

        _narrationService = App.Services.GetService<INarrationService>();
    }

    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        await _narrationService.PlayNarrationForPoi(_poi.Id.ToString());
    }
}
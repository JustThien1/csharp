using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Views;

public partial class PoiBottomSheet : ContentPage
{
    private readonly INarrationService _narration;
    private readonly IApiService _api;
    private POI? _poi;

    // Constructor cho DI
    public PoiBottomSheet(INarrationService narration, IApiService api)
    {
        InitializeComponent();
        _narration = narration;
        _api = api;
    }

    // Gọi sau khi tạo để set POI cần hiển thị
    public void SetPoi(POI poi)
    {
        _poi = poi;
        BindingContext = poi;
    }

    // Constructor tiện lợi khi truyền POI thẳng (dùng trong AppShell)
    public PoiBottomSheet(POI poi, INarrationService narration, IApiService api)
        : this(narration, api)
    {
        SetPoi(poi);
    }

    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        if (_poi is null) return;

        await _narration.PlayAsync(new NarrationRequest
        {
            Poi = _poi,
            Language = "vi",
            TriggerType = "manual",
            PreferAudioFile = true
        });

        // Log playback lên API
        var userId = Preferences.Get("userId", 0);
        await _api.LogPlaybackAsync(userId, _poi.Id, "manual");
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(animated: true);
    }
}

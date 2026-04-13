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

        var playback = App.Services.GetService<PlaybackService>();
        var userId = Preferences.Get("userId", 0);

        // 🔥 FIX: truyền thêm duration (có thể lấy từ audio length sau, tạm để 0 hoặc ước lượng)
        int duration = 60; // ví dụ 60 giây, sau bạn có thể lấy từ _narrationService

        await playback.LogPlayback(userId, _poi.Id, duration);
    }
    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        var collection = sender as CollectionView;

        if (collection?.SelectedItem is not Poi poi)
            return;

        // 🔥 reset trước (fix bug click)
        collection.SelectedItem = null;

        // 🔥 delay nhẹ để UI mượt hơn
        await Task.Delay(100);

        await Navigation.PushAsync(new PoiDetailPage(poi));
    }


}
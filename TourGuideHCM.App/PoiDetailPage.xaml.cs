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
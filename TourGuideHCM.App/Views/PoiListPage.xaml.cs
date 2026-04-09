using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services;

namespace TourGuideHCM.App.Views;

public partial class PoiListPage : ContentPage
{
    private readonly IDatabaseService _db;

    public PoiListPage()
    {
        InitializeComponent();
        _db = App.Services.GetService<IDatabaseService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var pois = await _db.GetAllPoisAsync();
        PoiList.ItemsSource = pois;
        PoiList.SelectionChanged += OnItemSelected;
    }
    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var poi = e.CurrentSelection.FirstOrDefault() as Poi;

            if (poi == null) return;

            // 🔥 chuyển sang trang chi tiết
            await Navigation.PushAsync(new PoiDetailPage(poi));

            // reset chọn
            ((CollectionView)sender).SelectedItem = null;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

}
using TourGuideHCM.Mobile.Models;

namespace TourGuideHCM.Mobile.Views;

public partial class POIDetailPage : ContentPage
{
    private readonly POI _poi;

    public POIDetailPage(POI poi)
    {
        InitializeComponent();
        _poi = poi;

        lblName.Text = poi.Name;
        lblAddress.Text = poi.Address;
        lblDescription.Text = poi.Description;
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_poi.AudioUrl))
            {
                await DisplayAlert("Thông báo", "Không có audio", "OK");
                return;
            }

            await Launcher.OpenAsync(_poi.AudioUrl);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }
}
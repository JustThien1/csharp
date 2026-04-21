using TourGuideHCM.App.Services.Interfaces;
using TourGuideHCM.App.Views;

namespace TourGuideHCM.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Đăng ký route cho các trang không nằm trong ShellContent
        Routing.RegisterRoute(nameof(PoiDetailPage), typeof(PoiDetailPage));
        Routing.RegisterRoute(nameof(PoiBottomSheet), typeof(PoiBottomSheet));

        // Khi Shell load xong → mở thẳng MapPage
        this.Loaded += async (s, e) =>
        {
            await GoToAsync("//MapPage");
        };
    }

    // Xử lý khi chọn POI từ Flyout
    private async void OnPoiFlyoutSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is not Models.POI selectedPoi)
            return;

        ((CollectionView)sender).SelectedItem = null;
        FlyoutIsPresented = false;

        var narration = App.Services.GetRequiredService<INarrationService>();
        var api = App.Services.GetRequiredService<IApiService>();

        var sheet = new PoiBottomSheet(selectedPoi, narration, api);
        await Navigation.PushModalAsync(sheet, animated: true);
    }

    private async void OnMapClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("//MapPage");
    }

    private async void OnPoiListClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("//PoiListPage");
    }
}
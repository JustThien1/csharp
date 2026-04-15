using TourGuideHCM.App.Views;

namespace TourGuideHCM.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Chỉ đăng ký các route KHÔNG có trong XAML
        Routing.RegisterRoute(nameof(PoiDetailPage), typeof(PoiDetailPage));
        Routing.RegisterRoute(nameof(PoiBottomSheet), typeof(PoiBottomSheet));

        this.Loaded += OnShellLoaded;
    }

    private async void OnShellLoaded(object? sender, EventArgs e)
    {
        var username = Preferences.Get("username", "");

        if (string.IsNullOrEmpty(username))
        {
            // Dùng absolute route tới FlyoutItem đã khai báo trong XAML
            await GoToAsync("//LoginPage");
        }
    }

    private async void OnPoiFlyoutSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is not Models.POI selectedPoi)
            return;

        ((CollectionView)sender).SelectedItem = null;
        FlyoutIsPresented = false;

        var narration = App.Services.GetRequiredService<Services.Interfaces.INarrationService>();
        var api = App.Services.GetRequiredService<Services.Interfaces.IApiService>();
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

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Remove("username");
        Preferences.Remove("userId");
        await GoToAsync("//LoginPage");
    }
}

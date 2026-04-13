using Microsoft.Maui.Storage;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services;
using TourGuideHCM.App.Views;

namespace TourGuideHCM.App;

public partial class AppShell : Shell
{
    private readonly IDatabaseService _databaseService;

    public AppShell(IDatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;

        // Hiển thị username trên title
        var username = Preferences.Get("username", "");
        if (!string.IsNullOrEmpty(username))
        {
            this.Title = "Xin chào " + username;
        }

        // Load danh sách POI vào Flyout
        this.Loaded += async (s, e) => await LoadPoisIntoFlyoutAsync();
    }

    // Tải POI vào Flyout
    private async Task LoadPoisIntoFlyoutAsync()
    {
        try
        {
            var pois = await _databaseService.GetAllPoisAsync();
            if (pois?.Count > 0)
            {
                PoiFlyoutList.ItemsSource = pois;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi load POI vào Flyout: {ex.Message}");
        }
    }

    // 🔥 Khi nhấn vào POI trong Flyout (3 gạch)
    private async void OnPoiFlyoutSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is not Poi selectedPoi)
            return;

        // Reset selection và đóng Flyout
        ((CollectionView)sender).SelectedItem = null;
        FlyoutIsPresented = false;

        // Hiển thị Bottom Sheet từ dưới lên (ưu tiên theo yêu cầu mới)
        var bottomSheet = new PoiBottomSheet(selectedPoi);
        await Shell.Current.Navigation.PushModalAsync(bottomSheet, animated: true);
    }

    // Các hàm điều hướng cũ (giữ lại để an toàn)
    private async void OnMapClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await Shell.Current.GoToAsync("///MapPage");
    }

    private async void OnPoiListClicked(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await Shell.Current.GoToAsync("///PoiListPage");
    }

    // Đăng xuất
    private void OnLogoutClicked(object sender, EventArgs e)
    {
        Preferences.Remove("username");
        Application.Current.MainPage = new NavigationPage(
            App.Services.GetService<LoginPage>()
        );
    }
}
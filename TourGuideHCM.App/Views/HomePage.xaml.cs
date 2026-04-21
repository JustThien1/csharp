using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;
using TourGuideHCM.App.ViewModels;

namespace TourGuideHCM.App.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;

    public HomePage(IApiService api, IDatabaseService db)
    {
        InitializeComponent();
        _vm = new HomeViewModel(api, db);
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAllAsync();
    }

    // ==================== POI list selection ====================
    private async void OnPoiSelected(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView cv) cv.SelectedItem = null;

        if (e.CurrentSelection.FirstOrDefault() is POI poi)
        {
            await Shell.Current.GoToAsync($"///MapPage?poiId={poi.Id}");
        }
    }

    // ==================== BOTTOM TAB BAR ====================

    /// <summary>Tab "Khám phá" — chính là trang hiện tại, chỉ scroll lên đầu.</summary>
    private void OnExploreTabTapped(object sender, TappedEventArgs e)
    {
        // Đã ở trang Home → cuộn lên đầu
        if (this.FindByName<ScrollView>("MainScrollView") is ScrollView sv)
        {
            sv.ScrollToAsync(0, 0, animated: true);
        }
    }

    /// <summary>Tab "Bản đồ" — chuyển sang MapPage.</summary>
    private async void OnMapTabTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("///MapPage");

    /// <summary>
    /// Tab giữa "🎧 Audio" — nút tròn nổi bật. Có thể mở audio tour chính,
    /// hoặc route tới PoiListPage đã lọc audio. Tạm điều hướng đến PoiListPage.
    /// </summary>
    private async void OnAudioTabTapped(object sender, TappedEventArgs e)
    {
        // TODO: khi có AudioTourPage riêng thì đổi route ở đây
        await Shell.Current.GoToAsync("///PoiListPage?hasAudio=1");
    }

    /// <summary>Tab "Đã lưu" — danh sách POI yêu thích.</summary>
    private async void OnFavoritesTabTapped(object sender, TappedEventArgs e)
    {
        // TODO: thay bằng FavoritesPage khi tạo xong
        await Shell.Current.GoToAsync("///PoiListPage?favorite=1");
    }

    /// <summary>Tab "Cá nhân" — trang profile.</summary>
    private async void OnProfileTabTapped(object sender, TappedEventArgs e)
    {
        // TODO: thay bằng ProfilePage khi tạo xong
        await DisplayAlert("Cá nhân", "Trang cá nhân đang được phát triển.", "OK");
    }
}

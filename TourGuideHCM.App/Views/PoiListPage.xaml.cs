using TourGuideHCM.App.ViewModels;

namespace TourGuideHCM.App.Views;

public partial class PoiListPage : ContentPage
{
    private readonly PoiViewModel _vm;

    public PoiListPage(PoiViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Navigate to detail when POI selected
        vm.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(PoiViewModel.SelectedPoi) && vm.SelectedPoi is not null)
            {
                await Shell.Current.GoToAsync(nameof(PoiDetailPage),
                    new Dictionary<string, object> { ["Poi"] = vm.SelectedPoi });
                vm.SelectedPoi = null;
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}

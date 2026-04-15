using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.ViewModels;

public class PoiViewModel : INotifyPropertyChanged
{
    private readonly IDatabaseService _db;
    private readonly IApiService _api;
    private readonly INarrationService _narration;

    public ObservableCollection<POI> Pois { get; } = new();
    public ObservableCollection<POI> FilteredPois { get; } = new();
    public ObservableCollection<string> Categories { get; } = new() { "Tất cả" };

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); FilterPois(); }
    }

    private string _selectedCategory = "Tất cả";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set { _selectedCategory = value; OnPropertyChanged(); FilterPois(); }
    }

    private POI? _selectedPoi;
    public POI? SelectedPoi
    {
        get => _selectedPoi;
        set { _selectedPoi = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public ICommand LoadCommand { get; }
    public ICommand SelectCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand RefreshCommand { get; }

    public PoiViewModel(IDatabaseService db, IApiService api, INarrationService narration)
    {
        _db = db;
        _api = api;
        _narration = narration;

        LoadCommand = new Command(async () => await LoadAsync());
        SelectCommand = new Command<POI>(poi => SelectedPoi = poi);
        PlayCommand = new Command<POI>(async poi => await PlayAsync(poi));
        RefreshCommand = new Command(async () => await LoadAsync(forceRefresh: true));
    }

    public async Task LoadAsync(bool forceRefresh = false)
    {
        IsLoading = true;
        try
        {
            List<POI> data;
            if (forceRefresh)
            {
                data = await _api.GetPoisAsync();
                if (data.Count > 0) await _db.UpsertPoisAsync(data);
            }
            else
            {
                data = await _db.GetCachedPoisAsync();
                if (data.Count == 0) data = await _api.GetPoisAsync();
            }

            Pois.Clear();
            foreach (var p in data.OrderBy(x => x.Priority).ThenBy(x => x.Name))
                Pois.Add(p);

            // Dùng CategoryName (string) – POI không có navigation property Category
            var cats = data
                .Select(p => p.CategoryName)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c);

            Categories.Clear();
            Categories.Add("Tất cả");
            foreach (var c in cats) Categories.Add(c);

            FilterPois();
        }
        finally { IsLoading = false; }
    }

    private void FilterPois()
    {
        FilteredPois.Clear();
        var result = Pois.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
            result = result.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        // Lọc theo CategoryName thay vì Category navigation property
        if (SelectedCategory != "Tất cả")
            result = result.Where(p => p.CategoryName == SelectedCategory);

        foreach (var p in result) FilteredPois.Add(p);
    }

    private async Task PlayAsync(POI poi)
    {
        await _narration.PlayAsync(new NarrationRequest
        {
            Poi = poi,
            Language = "vi",
            TriggerType = "manual",
            PreferAudioFile = true
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

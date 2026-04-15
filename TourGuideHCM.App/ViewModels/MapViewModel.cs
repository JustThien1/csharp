using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.ViewModels;

public class MapViewModel : INotifyPropertyChanged
{
    private readonly IApiService _api;
    private readonly IDatabaseService _db;
    private readonly IGeofenceService _geofence;
    private readonly INarrationService _narration;
    private readonly IAuthService _auth;

    public ObservableCollection<POI> Pois { get; } = new();

    private POI? _nearestPoi;
    public POI? NearestPoi
    {
        get => _nearestPoi;
        set { _nearestPoi = value; OnPropertyChanged(); }
    }

    private POI? _selectedPoi;
    public POI? SelectedPoi
    {
        get => _selectedPoi;
        set { _selectedPoi = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPoiDetailVisible)); }
    }

    private bool _isNarrating;
    public bool IsNarrating
    {
        get => _isNarrating;
        set { _isNarrating = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "Đang khởi động...";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private double _userLat = 10.7769;
    public double UserLat { get => _userLat; set { _userLat = value; OnPropertyChanged(); } }

    private double _userLng = 106.7009;
    public double UserLng { get => _userLng; set { _userLng = value; OnPropertyChanged(); } }

    private string _selectedLanguage = "vi";
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set { _selectedLanguage = value; OnPropertyChanged(); }
    }

    private bool _preferAudioFile = true;
    public bool PreferAudioFile
    {
        get => _preferAudioFile;
        set { _preferAudioFile = value; OnPropertyChanged(); }
    }

    public bool IsPoiDetailVisible => SelectedPoi is not null;

    public ICommand LoadPoisCommand { get; }
    public ICommand SelectPoiCommand { get; }
    public ICommand ClosePoiDetailCommand { get; }
    public ICommand PlayNarrationCommand { get; }
    public ICommand StopNarrationCommand { get; }
    public ICommand ToggleLanguageCommand { get; }

    public MapViewModel(IApiService api, IDatabaseService db,
        IGeofenceService geofence, INarrationService narration, IAuthService auth)
    {
        _api = api;
        _db = db;
        _geofence = geofence;
        _narration = narration;
        _auth = auth;

        LoadPoisCommand = new Command(async () => await LoadPoisAsync());
        SelectPoiCommand = new Command<POI>(p => SelectedPoi = p);
        ClosePoiDetailCommand = new Command(() => SelectedPoi = null);
        PlayNarrationCommand = new Command<POI>(async p =>
        {
            if (p is not null) await PlayNarrationAsync(p);
        });
        StopNarrationCommand = new Command(async () => await _narration.StopAsync());
        ToggleLanguageCommand = new Command(() =>
            SelectedLanguage = SelectedLanguage == "vi" ? "en" : "vi");

        _geofence.GeofenceTriggered += OnGeofenceTriggered;
        _geofence.LocationUpdated += OnLocationUpdated;

        _narration.NarrationStarted += (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => IsNarrating = true);
        _narration.NarrationCompleted += (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => IsNarrating = false);
    }

    public async Task InitializeAsync()
    {
        await _db.InitAsync();
        await LoadPoisAsync();
    }

    private async Task LoadPoisAsync()
    {
        IsLoading = true;
        StatusMessage = "Đang tải điểm tham quan...";
        try
        {
            // Hiển thị cache trước
            var cached = await _db.GetCachedPoisAsync();
            if (cached.Count > 0) UpdateCollection(cached);

            // Tải mới từ API
            var fresh = await _api.GetPoisAsync();
            if (fresh.Count > 0)
            {
                await _db.UpsertPoisAsync(fresh);
                UpdateCollection(fresh);
                StatusMessage = $"Đã tải {fresh.Count} điểm tham quan";
            }
            else if (cached.Count > 0)
            {
                StatusMessage = $"{cached.Count} điểm (offline)";
            }
            else
            {
                StatusMessage = "Không có dữ liệu";
            }

            // Bắt đầu geofence – bọc try-catch riêng,
            // không để lỗi GPS permission crash toàn app
            if (Pois.Count > 0)
            {
                try
                {
                    await _geofence.StartAsync(Pois);
                }
                catch (UnauthorizedAccessException)
                {
                    StatusMessage = "Chưa cấp quyền vị trí – thuyết minh tự động bị tắt";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Geofence] {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateCollection(List<POI> pois)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Pois.Clear();
            foreach (var p in pois.OrderBy(x => x.Priority))
                Pois.Add(p);
        });
    }

    private async void OnGeofenceTriggered(object? sender, GeofenceTriggeredEventArgs e)
    {
        NearestPoi = e.Poi;
        foreach (var p in Pois) p.IsHighlighted = p.Id == e.Poi.Id;
        await PlayNarrationAsync(e.Poi, e.TriggerType);
    }

    private void OnLocationUpdated(object? sender, LocationUpdate e)
    {
        UserLat = e.Lat;
        UserLng = e.Lng;

        if (Pois.Count == 0) return;

        POI? nearest = null;
        double minDist = double.MaxValue;

        foreach (var poi in Pois)
        {
            poi.DistanceMeters = _geofence.CalculateDistance(e.Lat, e.Lng, poi.Lat, poi.Lng);
            if (poi.DistanceMeters < minDist)
            {
                minDist = poi.DistanceMeters.Value;
                nearest = poi;
            }
        }

        if (nearest is not null)
        {
            NearestPoi = nearest;
            foreach (var p in Pois) p.IsHighlighted = p.Id == nearest.Id;
        }
    }

    public async Task PlayNarrationAsync(POI poi, string triggerType = "manual")
    {
        if (IsNarrating) return;

        await _narration.PlayAsync(new NarrationRequest
        {
            Poi = poi,
            Language = SelectedLanguage,
            TriggerType = triggerType,
            PreferAudioFile = PreferAudioFile
        });

        var userId = _auth.CurrentUser?.Id ?? 0;
        _ = _api.LogPlaybackAsync(userId, poi.Id, triggerType);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

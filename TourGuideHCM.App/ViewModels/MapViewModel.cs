using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services;
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

    // ── Nearest POI ───────────────────────────────────────────────────────────
    private POI? _nearestPoi;
    public POI? NearestPoi
    {
        get => _nearestPoi;
        set { _nearestPoi = value; OnPropertyChanged(); OnPropertyChanged(nameof(NearestPoiVisible)); }
    }
    public bool NearestPoiVisible => NearestPoi is not null;

    // ── Selected POI ──────────────────────────────────────────────────────────
    private POI? _selectedPoi;
    public POI? SelectedPoi
    {
        get => _selectedPoi;
        set { _selectedPoi = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPoiDetailVisible)); }
    }
    public bool IsPoiDetailVisible => SelectedPoi is not null;

    // ── Trạng thái ────────────────────────────────────────────────────────────
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

    // ── Vị trí user ──────────────────────────────────────────────────────────
    private double _userLat = 10.7769;
    public double UserLat { get => _userLat; set { _userLat = value; OnPropertyChanged(); } }

    private double _userLng = 106.7009;
    public double UserLng { get => _userLng; set { _userLng = value; OnPropertyChanged(); } }

    // ── Ngôn ngữ thuyết minh (vi/en) ─────────────────────────────────────────
    private string _selectedLanguage = "vi";
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set { _selectedLanguage = value; OnPropertyChanged(); }
    }

    public bool PreferAudioFile { get; set; } = true;

    // ── Bán kính kích hoạt TTS ────────────────────────────────────────────────
    private double _activationRadius = 100;
    public double ActivationRadius
    {
        get => _activationRadius;
        set
        {
            _activationRadius = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActivationRadiusLabel));
            foreach (var poi in Pois)
                poi.Radius = value;
        }
    }
    public string ActivationRadiusLabel => $"{_activationRadius:F0}m";

    // ── Ngôn ngữ UI (từ LanguageService) ─────────────────────────────────────
    public string LangToggleLabel => LanguageService.Instance.ToggleLabel;

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand LoadPoisCommand { get; }
    public ICommand SelectPoiCommand { get; }
    public ICommand ClosePoiDetailCommand { get; }
    public ICommand PlayNarrationCommand { get; }
    public ICommand StopNarrationCommand { get; }
    public ICommand ToggleLanguageCommand { get; }
    public ICommand ToggleAppLanguageCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }

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

        // Toggle ngôn ngữ thuyết minh (vi/en)
        ToggleLanguageCommand = new Command(() =>
            SelectedLanguage = SelectedLanguage == "vi" ? "en" : "vi");

        // Toggle ngôn ngữ UI toàn app
        ToggleAppLanguageCommand = new Command(() =>
        {
            LanguageService.Instance.Toggle();
            // Đồng bộ ngôn ngữ thuyết minh theo ngôn ngữ UI
            SelectedLanguage = LanguageService.IsEnglish ? "en" : "vi";
        });

        // Yêu thích
        ToggleFavoriteCommand = new Command<POI>(async p =>
        {
            if (p is null) return;
            p.IsFavorite = !p.IsFavorite;
            OnPropertyChanged(nameof(SelectedPoi));
            OnPropertyChanged(nameof(NearestPoi));
            await _db.UpsertPoisAsync(new List<POI> { p });
        });

        _geofence.GeofenceTriggered += OnGeofenceTriggered;
        _geofence.LocationUpdated += OnLocationUpdated;

        _narration.NarrationStarted += (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => IsNarrating = true);
        _narration.NarrationCompleted += (_, _) =>
            MainThread.BeginInvokeOnMainThread(() => IsNarrating = false);

        // Khi ngôn ngữ UI thay đổi → refresh text
        LanguageService.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshLanguage);

        // Lắng nghe deep link tourguide://poi/{id}
        DeepLinkService.PoiRequested += async (_, poiId) =>
        {
            // Đợi POI load xong nếu chưa có
            var retry = 0;
            while (!Pois.Any() && retry++ < 10)
                await Task.Delay(500);

            var poi = Pois.FirstOrDefault(p => p.Id == poiId);
            if (poi != null)
            {
                MainThread.BeginInvokeOnMainThread(() => SelectedPoi = poi);
                await PlayNarrationAsync(poi, "deeplink");
            }
        };
    }

    // ── Language refresh ──────────────────────────────────────────────────────
    private void RefreshLanguage()
    {
        // Đồng bộ ngôn ngữ thuyết minh
        SelectedLanguage = LanguageService.IsEnglish ? "en" : "vi";
        OnPropertyChanged(nameof(LangToggleLabel));
        // Refresh status message theo ngôn ngữ mới
        if (!IsLoading)
            StatusMessage = string.Format(AppLanguage.Loaded, Pois.Count);
    }

    // ── Init ──────────────────────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        await _db.InitAsync();
        await LoadPoisAsync();
    }

    private async Task LoadPoisAsync()
    {
        IsLoading = true;
        StatusMessage = AppLanguage.Loading;
        try
        {
            var cached = await _db.GetCachedPoisAsync();
            if (cached.Count > 0) UpdateCollection(cached);

            var fresh = await _api.GetPoisAsync();
            if (fresh.Count > 0)
            {
                foreach (var f in fresh)
                {
                    var local = cached.FirstOrDefault(c => c.Id == f.Id);
                    if (local is not null) f.IsFavorite = local.IsFavorite;
                }
                await _db.UpsertPoisAsync(fresh);
                UpdateCollection(fresh);
                StatusMessage = string.Format(AppLanguage.Loaded, fresh.Count);
            }
            else if (cached.Count > 0)
            {
                StatusMessage = string.Format(AppLanguage.Offline, cached.Count);
            }
            else
            {
                StatusMessage = AppLanguage.NoData;
            }

            if (Pois.Count > 0)
            {
                try { await _geofence.StartAsync(Pois); }
                catch (UnauthorizedAccessException)
                { StatusMessage = AppLanguage.NoPermission; }
                catch (Exception ex)
                { System.Diagnostics.Debug.WriteLine($"[Geofence] {ex.Message}"); }
            }
        }
        catch (Exception ex) { StatusMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
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
        if (e.Poi.DistanceMeters > _activationRadius) return;
        NearestPoi = e.Poi;
        HighlightNearest(e.Poi.Id);
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
            HighlightNearest(nearest.Id);
            _ = LogRouteLocation(e.Lat, e.Lng);  // Lưu tuyến đường
        }
    }

    private DateTime _lastRouteLog = DateTime.MinValue;

    private async Task LogRouteLocation(double lat, double lng)
    {
        // Log mỗi 30 giây để tránh spam DB
        if ((DateTime.UtcNow - _lastRouteLog).TotalSeconds < 30) return;
        _lastRouteLog = DateTime.UtcNow;
        var userId = _auth.CurrentUser?.Id ?? 0;
        await _api.LogRouteAsync(userId, lat, lng);
    }

    private void HighlightNearest(int poiId)
    {
        foreach (var p in Pois)
            p.IsHighlighted = p.Id == poiId;
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
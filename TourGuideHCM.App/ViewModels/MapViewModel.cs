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
    private readonly IDeviceInfoService _deviceInfo;
    private readonly IAudioQueueService _audioQueue;

    public ObservableCollection<POI> Pois { get; } = new();

    // Nearest POI
    private POI? _nearestPoi;
    public POI? NearestPoi
    {
        get => _nearestPoi;
        set { _nearestPoi = value; OnPropertyChanged(); OnPropertyChanged(nameof(NearestPoiVisible)); }
    }
    public bool NearestPoiVisible => NearestPoi is not null;

    // Selected POI
    private POI? _selectedPoi;
    public POI? SelectedPoi
    {
        get => _selectedPoi;
        set { _selectedPoi = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPoiDetailVisible)); }
    }
    public bool IsPoiDetailVisible => SelectedPoi is not null;

    private bool _isNarrating;
    public bool IsNarrating
    {
        get => _isNarrating;
        set { _isNarrating = value; OnPropertyChanged(); }
    }

    // ====================== AUDIO QUEUE (MỚI) ======================
    /// <summary>Queue các POI đang chờ phát — UI bind để hiển thị "Sắp tới"</summary>
    public System.Collections.ObjectModel.ReadOnlyObservableCollection<QueuedPoi> AudioQueue => _audioQueue.Queue;

    /// <summary>POI đang phát hiện tại (null nếu không có) — UI hiển thị "Đang phát"</summary>
    public QueuedPoi? CurrentPlayingPoi => _audioQueue.Current;

    /// <summary>True nếu có bất kỳ POI nào trong queue (đang phát hoặc chờ)</summary>
    public bool HasQueue => _audioQueue.Current != null || _audioQueue.Queue.Count > 0;

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

    public bool PreferAudioFile { get; set; } = true;

    private double _activationRadius = 100;
    public double ActivationRadius
    {
        get => _activationRadius;
        set
        {
            _activationRadius = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ActivationRadiusLabel));
            foreach (var poi in Pois) poi.Radius = value;
        }
    }
    public string ActivationRadiusLabel => $"{_activationRadius:F0}m";

    public string LangToggleLabel => LanguageService.Instance.ToggleLabel;

    // Commands
    public ICommand LoadPoisCommand { get; }
    public ICommand SelectPoiCommand { get; }
    public ICommand ClosePoiDetailCommand { get; }
    public ICommand PlayNarrationCommand { get; }
    public ICommand StopNarrationCommand { get; }
    public ICommand ToggleLanguageCommand { get; }
    public ICommand ToggleAppLanguageCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }

    // ====================== AUDIO QUEUE COMMANDS ======================
    public ICommand SkipCurrentCommand { get; }
    public ICommand StopQueueCommand { get; }
    public ICommand ToggleQueueExpandedCommand { get; }

    private bool _isQueueExpanded = false;
    /// <summary>True = hiện full card; False = chỉ hiện icon nhỏ để phát.</summary>
    public bool IsQueueExpanded
    {
        get => _isQueueExpanded;
        set { _isQueueExpanded = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsQueueCollapsed)); }
    }
    public bool IsQueueCollapsed => !_isQueueExpanded;

    public MapViewModel(IApiService api, IDatabaseService db,
        IGeofenceService geofence, INarrationService narration,
        IDeviceInfoService deviceInfo,
        IAudioQueueService audioQueue)
    {
        _api = api;
        _db = db;
        _geofence = geofence;
        _narration = narration;
        _deviceInfo = deviceInfo;
        _audioQueue = audioQueue;

        LoadPoisCommand = new Command(async () => await LoadPoisAsync());
        SelectPoiCommand = new Command<POI>(p => SelectedPoi = p);
        ClosePoiDetailCommand = new Command(() => SelectedPoi = null);

        PlayNarrationCommand = new Command<POI>(async p =>
        {
            if (p is not null) await PlayNarrationAsync(p);
        });

        StopNarrationCommand = new Command(async () => await _narration.StopAsync());

        // ====================== AUDIO QUEUE COMMANDS ======================
        SkipCurrentCommand = new Command(() => _audioQueue.Skip());
        StopQueueCommand = new Command(() => _audioQueue.StopAll());
        ToggleQueueExpandedCommand = new Command(() => IsQueueExpanded = !IsQueueExpanded);

        ToggleLanguageCommand = new Command(() =>
            SelectedLanguage = SelectedLanguage == "vi" ? "en" : "vi");

        ToggleAppLanguageCommand = new Command(() =>
        {
            LanguageService.Instance.Toggle();
            SelectedLanguage = LanguageService.IsEnglish ? "en" : "vi";
        });

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
        _geofence.PoisInRangeChanged += OnPoisInRangeChanged;

        // Khi queue thay đổi → raise property changed để UI binding cập nhật
        _audioQueue.QueueChanged += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(AudioQueue));
            OnPropertyChanged(nameof(CurrentPlayingPoi));
            OnPropertyChanged(nameof(HasQueue));
        });

        _narration.NarrationStarted += (_, _) => MainThread.BeginInvokeOnMainThread(() => IsNarrating = true);
        _narration.NarrationCompleted += (_, _) => MainThread.BeginInvokeOnMainThread(() => IsNarrating = false);

        LanguageService.LanguageChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshLanguage);
    }

    public async Task InitializeAsync()
    {
        await _db.InitAsync();
        await LoadPoisAsync();
    }

    // ====================== LOG ONLINE KHI MỞ APP ======================
    public async Task LogUserOnlineAsync()
    {
        try
        {
            // Gửi log để Monitoring biết user đang online (với DeviceId thật)
            await _api.LogPlaybackAsync(
                _deviceInfo.CurrentUserId, 0, "online",
                deviceId: _deviceInfo.DeviceId,
                deviceName: _deviceInfo.DeviceName,
                platform: _deviceInfo.Platform);
            Console.WriteLine($"📡 Logged online: {_deviceInfo.DeviceName} ({_deviceInfo.Platform})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Log online error: {ex.Message}");
        }
    }

    // ====================== PLAY NARRATION (KHÔNG CÒN SIGNALR) ======================
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

        // Log playback để Monitoring lấy dữ liệu (kèm DeviceId để phân biệt thiết bị)
        await _api.LogPlaybackAsync(
            _deviceInfo.CurrentUserId, poi.Id, triggerType,
            deviceId: _deviceInfo.DeviceId,
            deviceName: _deviceInfo.DeviceName,
            platform: _deviceInfo.Platform);
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
                catch (UnauthorizedAccessException) { StatusMessage = AppLanguage.NoPermission; }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Geofence] {ex.Message}"); }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
        }
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

    /// <summary>
    /// Sự kiện cũ: giữ lại cho tương thích ngược. Giờ không tự phát nữa —
    /// AudioQueue tự xử lý qua OnPoisInRangeChanged.
    /// </summary>
    private void OnGeofenceTriggered(object? sender, GeofenceTriggeredEventArgs e)
    {
        if (e.Poi.DistanceMeters > _activationRadius) return;
        NearestPoi = e.Poi;
        HighlightNearest(e.Poi.Id);
    }

    /// <summary>
    /// MỚI: GeofenceService phát sự kiện chứa TẤT CẢ POI trong vùng.
    /// Filter theo activationRadius rồi feed vào AudioQueue.
    /// Queue tự lo việc phát, xếp lịch, cooldown.
    /// </summary>
    private void OnPoisInRangeChanged(object? sender, PoisInRangeEventArgs e)
    {
        // Chỉ giữ các POI trong activationRadius của user (setting UI)
        var filtered = e.PoisInRange
            .Where(x => x.distance <= _activationRadius)
            .ToList();

        // Highlight POI gần nhất trên map
        var nearest = filtered.OrderBy(x => x.distance).FirstOrDefault();
        if (nearest.poi != null)
        {
            NearestPoi = nearest.poi;
            HighlightNearest(nearest.poi.Id);
        }
        else
        {
            NearestPoi = null;
        }

        // Feed vào queue
        _audioQueue.UpdateInRangePois(filtered, SelectedLanguage);
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
            _ = LogRouteLocation(e.Lat, e.Lng);
        }
    }

    private DateTime _lastRouteLog = DateTime.MinValue;

    private async Task LogRouteLocation(double lat, double lng)
    {
        if ((DateTime.UtcNow - _lastRouteLog).TotalSeconds < 30) return;
        _lastRouteLog = DateTime.UtcNow;
        // Gửi kèm DeviceId để Admin có thể tìm tuyến đường theo thiết bị (app không có login)
        await _api.LogRouteAsync(
            _deviceInfo.CurrentUserId,
            lat, lng,
            deviceId: _deviceInfo.DeviceId);
    }

    private void HighlightNearest(int poiId)
    {
        foreach (var p in Pois)
            p.IsHighlighted = p.Id == poiId;
    }

    private void RefreshLanguage()
    {
        SelectedLanguage = LanguageService.IsEnglish ? "en" : "vi";
        OnPropertyChanged(nameof(LangToggleLabel));
        if (!IsLoading)
            StatusMessage = string.Format(AppLanguage.Loaded, Pois.Count);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
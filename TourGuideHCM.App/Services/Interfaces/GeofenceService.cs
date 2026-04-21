using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

public class GeofenceService : IGeofenceService, IDisposable
{
    private readonly IDatabaseService _db;
    private readonly IApiService _api;

    private const double NearbyMultiplier = 2.0;
    private const int PollingIntervalMs = 5000;

    private List<POI> _pois = new();
    private readonly Dictionary<int, bool> _insideZone = new();

    private CancellationTokenSource? _cts;
    private Task? _pollingTask;

    public bool IsRunning { get; private set; }

    // Event cũ giữ lại để tương thích ngược — nhưng logic mới dùng PoisInRangeChanged
    public event EventHandler<GeofenceTriggeredEventArgs>? GeofenceTriggered;
    public event EventHandler<LocationUpdate>? LocationUpdated;

    /// <summary>
    /// MỚI: sự kiện phát khi tập hợp POI trong vùng thay đổi.
    /// MapViewModel sẽ subscribe vào đây và feed vào AudioQueueService.
    /// </summary>
    public event EventHandler<PoisInRangeEventArgs>? PoisInRangeChanged;

    public GeofenceService(IDatabaseService db, IApiService api)
    {
        _db = db;
        _api = api;
    }

    public async Task StartAsync(IEnumerable<POI> pois)
    {
        if (IsRunning) return;
        _pois = pois.Where(p => p.IsActive).ToList();
        _cts = new CancellationTokenSource();
        IsRunning = true;

        await RequestPermissionAsync();
        _pollingTask = Task.Run(() => PollingLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        IsRunning = false;
        _cts?.Cancel();
        if (_pollingTask is not null)
        {
            try { await _pollingTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
        _pollingTask = null;
    }

    public Task UpdatePoisAsync(IEnumerable<POI> pois)
    {
        _pois = pois.Where(p => p.IsActive).ToList();
        return Task.CompletedTask;
    }

    private static async Task RequestPermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            throw new UnauthorizedAccessException(
                "Cần quyền vị trí để dùng tính năng thuyết minh tự động.");
    }

    private async Task PollingLoopAsync(CancellationToken token)
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Best,
            TimeSpan.FromSeconds(5));

        while (!token.IsCancellationRequested)
        {
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(request, token);
                if (location is not null)
                {
                    var update = new LocationUpdate
                    {
                        Lat = location.Latitude,
                        Lng = location.Longitude,
                        Accuracy = location.Accuracy ?? 0
                    };

                    MainThread.BeginInvokeOnMainThread(() =>
                        LocationUpdated?.Invoke(this, update));

                    await CheckGeofencesAsync(update);
                }

                await Task.Delay(PollingIntervalMs, token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Geofence] {ex.Message}");
                await Task.Delay(PollingIntervalMs, token);
            }
        }
    }

    /// <summary>
    /// Thay vì chỉ tìm 1 POI gần nhất, giờ build ra DANH SÁCH tất cả POI
    /// trong vùng activation. AudioQueue sẽ xử lý queue và cooldown riêng.
    /// </summary>
    private async Task CheckGeofencesAsync(LocationUpdate loc)
    {
        if (_pois.Count == 0) return;

        var inRange = new List<(POI poi, double distance)>();

        foreach (var poi in _pois)
        {
            var dist = CalculateDistance(loc.Lat, loc.Lng, poi.Lat, poi.Lng);
            poi.DistanceMeters = dist;

            var r = poi.Radius > 0 ? poi.Radius : 100.0;
            var isInside = dist <= r;

            if (isInside)
                inRange.Add((poi, dist));

            // Log enter event để DB có record (cho heatmap, thống kê)
            var wasInside = _insideZone.GetValueOrDefault(poi.Id, false);
            if (isInside && !wasInside)
            {
                await _db.AddGeofenceEventAsync(new GeofenceEvent
                { PoiId = poi.Id, EventType = "enter", DistanceAtTrigger = dist });
            }
            _insideZone[poi.Id] = isInside;
        }

        // Phát sự kiện cho MapViewModel feed vào queue
        MainThread.BeginInvokeOnMainThread(() =>
            PoisInRangeChanged?.Invoke(this, new PoisInRangeEventArgs
            {
                PoisInRange = inRange,
                UserLat = loc.Lat,
                UserLng = loc.Lng
            }));
    }

    public double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000;
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2)
               + Math.Cos(φ1) * Math.Cos(φ2)
               * Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}

public class PoisInRangeEventArgs : EventArgs
{
    public List<(POI poi, double distance)> PoisInRange { get; set; } = new();
    public double UserLat { get; set; }
    public double UserLng { get; set; }
}

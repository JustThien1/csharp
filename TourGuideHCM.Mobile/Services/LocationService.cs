using Microsoft.Maui.Devices.Sensors;
using TourGuideHCM.Mobile.Models;

namespace TourGuideHCM.Mobile.Services;

public class LocationService
{
    private bool _isTracking = false;
    private CancellationTokenSource? _cts;
    private readonly Dictionary<int, DateTime> _lastTriggered = new();

    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    return null;
            }

            return await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.High,
                Timeout = TimeSpan.FromSeconds(10)
            });
        }
        catch
        {
            return null;
        }
    }

    public async Task StartGeofenceTrackingAsync(List<POI> pois, Action<POI> onPOIEntered)
    {
        if (_isTracking || pois.Count == 0) return;

        _isTracking = true;
        _cts = new CancellationTokenSource();

        while (!_cts.IsCancellationRequested)
        {
            var location = await GetCurrentLocationAsync();

            if (location != null)
            {
                foreach (var poi in pois)
                {
                    var distance = Location.CalculateDistance(
                        location,
                        new Location(poi.Lat, poi.Lng),
                        DistanceUnits.Kilometers);

                    if (distance <= poi.TriggerRadius)
                    {
                        // chống spam (cooldown 30s)
                        if (_lastTriggered.TryGetValue(poi.Id, out var lastTime))
                        {
                            if ((DateTime.Now - lastTime).TotalSeconds < 30)
                                continue;
                        }

                        _lastTriggered[poi.Id] = DateTime.Now;
                        onPOIEntered(poi);
                    }
                }
            }

            await Task.Delay(3000);
        }
    }

    public void StopTracking()
    {
        _cts?.Cancel();
        _isTracking = false;
    }
}
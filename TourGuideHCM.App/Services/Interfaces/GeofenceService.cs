using Shiny.Locations;

namespace TourGuideHCM.App.Services;

public class GeofenceService : IGeofenceService
{
    private readonly IGeofenceManager _geofenceManager;

    public GeofenceService(IGeofenceManager geofenceManager)
    {
        _geofenceManager = geofenceManager;
    }

    public async Task StartMonitoringAsync(GeofenceRegion region)
    {
        await _geofenceManager.StartMonitoring(region);
    }

    // Thêm hàm này để dễ dùng sau này
    public async Task StopAllMonitoringAsync()
    {
        await _geofenceManager.StopAllMonitoring();
    }
}
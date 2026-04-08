using Shiny.Locations;

namespace TourGuideHCM.App.Services;

public interface IGeofenceService
{
    Task StartMonitoringAsync(GeofenceRegion region);
}
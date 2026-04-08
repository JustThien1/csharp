using Shiny.Locations;
using TourGuideHCM.App.Services;
using Microsoft.Extensions.Logging;

namespace TourGuideHCM.App.Services;

public class GeofenceDelegate : IGeofenceDelegate
{
    private readonly INarrationService _narrationService;
    private readonly ILogger<GeofenceDelegate> _logger;

    public GeofenceDelegate(INarrationService narrationService, ILogger<GeofenceDelegate> logger)
    {
        _narrationService = narrationService;
        _logger = logger;
    }

    public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
        if (newStatus == GeofenceState.Entered)
        {
            _logger.LogInformation("Geofence entered: {Identifier}", region.Identifier);
            await _narrationService.PlayNarrationForPoi(region.Identifier);
        }
        else if (newStatus == GeofenceState.Exited)
        {
            _logger.LogInformation("Geofence exited: {Identifier}", region.Identifier);
        }
    }
}
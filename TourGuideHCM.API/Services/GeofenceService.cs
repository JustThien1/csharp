using TourGuideHCM.API.Helpers;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services
{
    public class GeofenceService
    {
        public POI? GetNearestPOI(double lat, double lng, List<POI> pois)
        {
            var result = pois
                .Select(p => new
                {
                    POI = p,
                    Distance = HaversineHelper.CalculateDistance(lat, lng, p.Lat, p.Lng)
                })
                .Where(x => x.Distance <= x.POI.Radius)
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            return result?.POI;
        }
    }
}
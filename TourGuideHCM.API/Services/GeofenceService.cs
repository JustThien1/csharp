using TourGuideHCM.API.Helpers;
using TourGuideHCM.API.Models;
using TourGuideHCM.API.Data;

namespace TourGuideHCM.API.Services
{
    public class GeofenceService
    {
        private readonly AppDbContext _context;

        public GeofenceService(AppDbContext context)
        {
            _context = context;
        }

        // ====================== GetNearby ======================
        public POI? GetNearestPOI(double lat, double lng, List<POI> pois)
        {
            if (pois == null || pois.Count == 0)
                return null;

            var result = pois
                .Select(p => new
                {
                    POI = p,
                    Distance = HaversineHelper.CalculateDistance(lat, lng, p.Lat, p.Lng)
                })
                .Where(x => x.Distance <= (x.POI.Radius > 0 ? x.POI.Radius : 100.0))
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            return result?.POI;
        }

        // ====================== Trigger Geofence (quan trọng cho đồ án) ======================
        public POI? GetTriggeredPOI(double lat, double lng, int userId)
        {
            var pois = _context.POIs
                .Where(p => p.IsActive)
                .ToList();

            if (pois.Count == 0)
                return null;

            var candidates = pois
                .Select(p => new
                {
                    POI = p,
                    Distance = HaversineHelper.CalculateDistance(lat, lng, p.Lat, p.Lng)
                })
                .Where(x => x.Distance <= (x.POI.Radius > 0 ? x.POI.Radius : 100.0))
                .OrderBy(x => x.POI.Priority)
                .ThenBy(x => x.Distance)
                .ToList();

            if (!candidates.Any())
                return null;

            // Chống spam: cooldown 5 phút
            var lastTrigger = _context.PlaybackLogs
                .Where(l => l.POIId == candidates[0].POI.Id && l.UserId == userId)
                .OrderByDescending(l => l.TriggeredAt)
                .FirstOrDefault();

            if (lastTrigger != null && (DateTime.UtcNow - lastTrigger.TriggeredAt).TotalMinutes < 5)
                return null;

            return candidates[0].POI;
        }
    }
}
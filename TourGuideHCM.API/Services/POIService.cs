using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services
{
    public class POIService
    {
        private readonly AppDbContext _context;

        public POIService(AppDbContext context)
        {
            _context = context;
        }

        public List<POI> GetAll()
        {
            return _context.POIs
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority)
                .ToList();
        }

        public POI? GetById(int id)
        {
            return _context.POIs
                .Include(p => p.Category)
                .FirstOrDefault(x => x.Id == id);
        }

        public POI Add(POI poi)
        {
            // 🔥 Validate CategoryId
            var categoryExists = _context.Categories.Any(c => c.Id == poi.CategoryId);
            if (!categoryExists)
                throw new Exception("CategoryId không tồn tại");

            poi.IsActive = true;

            _context.POIs.Add(poi);
            _context.SaveChanges();

            return poi;
        }

        public bool Update(int id, POI updated)
        {
            var poi = _context.POIs.Find(id);
            if (poi == null) return false;

            poi.Name = updated.Name;
            poi.Description = updated.Description;
            poi.Address = updated.Address;
            poi.Lat = updated.Lat;
            poi.Lng = updated.Lng;
            poi.Radius = updated.Radius;
            poi.Priority = updated.Priority;
            poi.ImageUrl = updated.ImageUrl;
            poi.AudioUrl = updated.AudioUrl;
            poi.NarrationText = updated.NarrationText;
            poi.Language = updated.Language ?? "vi";
            poi.OpeningHours = updated.OpeningHours;
            poi.TicketPrice = updated.TicketPrice;
            poi.IsActive = updated.IsActive;
            poi.CategoryId = updated.CategoryId;

            _context.SaveChanges();
            return true;
        }

        public bool Delete(int id)
        {
            var poi = _context.POIs.Find(id);
            if (poi == null) return false;

            _context.POIs.Remove(poi);
            _context.SaveChanges();
            return true;
        }

        public void LogPlayback(int userId, int poiId, string triggerType)
        {
            var log = new PlaybackLog
            {
                UserId = userId,
                POIId = poiId,
                TriggerType = triggerType,
                TriggeredAt = DateTime.UtcNow
            };

            _context.PlaybackLogs.Add(log);
            _context.SaveChanges();
        }

        public POI? GetByQRCode(string code)
        {
            if (int.TryParse(code, out int id))
                return GetById(id);

            return _context.POIs.FirstOrDefault(p => p.Name.Contains(code));
        }
    }
}
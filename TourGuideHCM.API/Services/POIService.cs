using TourGuideHCM.API.Models;
using TourGuideHCM.API.Repositories; // ⚠️ BẮT BUỘC

namespace TourGuideHCM.API.Services
{
    public class POIService
    {
        private readonly IPOIRepository _repo;

        public POIService(IPOIRepository repo)
        {
            _repo = repo;
        }

        public List<POI> GetAll()
        {
            return _repo.GetAll();
        }
    }
}
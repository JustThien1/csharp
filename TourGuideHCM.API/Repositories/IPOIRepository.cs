using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Repositories
{
    public interface IPOIRepository
    {
        List<POI> GetAll();
    }
}
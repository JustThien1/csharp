using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Repositories
{
    public class POIRepository : IPOIRepository
    {
        public List<POI> GetAll()
        {
            return new List<POI>
            {
                new POI
                {
                    Id = 1,
                    Name = "Nhà thờ Đức Bà",
                    Description = "Địa điểm nổi tiếng",
                    Lat = 10.779783,
                    Lng = 106.699018
                }
            };
        }
    }
}
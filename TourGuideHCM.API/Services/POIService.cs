using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services
{
    public class POIService
    {
        private static List<POI> _data = new List<POI>
        {
            new POI
            {
                Id = 1,
                Name = "Nhà thờ Đức Bà",
                Description = "Địa điểm nổi tiếng",
                Lat = 10.779783,
                Lng = 106.699018,
                Radius = 100
            }
        };

        public List<POI> GetAll() => _data;

        public POI? GetById(int id) => _data.FirstOrDefault(x => x.Id == id);

        public void Add(POI poi)
        {
            poi.Id = _data.Count + 1;
            _data.Add(poi);
        }

        public bool Update(int id, POI updated)
        {
            var poi = _data.FirstOrDefault(x => x.Id == id);
            if (poi == null) return false;

            poi.Name = updated.Name;
            poi.Description = updated.Description;
            poi.Lat = updated.Lat;
            poi.Lng = updated.Lng;
            poi.Radius = updated.Radius;

            return true;
        }

        public bool Delete(int id)
        {
            var poi = _data.FirstOrDefault(x => x.Id == id);
            if (poi == null) return false;

            _data.Remove(poi);
            return true;
        }
    }
}
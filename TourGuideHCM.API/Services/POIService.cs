using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services
{
    public class POIService
    {
        private static readonly List<POI> _data = new()
        {
            new POI
            {
                Id = 1,
                Name = "Nhà thờ Đức Bà",
                Description = "Địa điểm nổi tiếng tại TP.HCM",
                Lat = 10.779783,
                Lng = 106.699018,
                Radius = 100,
                AudioUrl = "audio/nhatho.mp3"
            },
            new POI
            {
                Id = 2,
                Name = "Bưu điện TP.HCM",
                Description = "Công trình kiến trúc cổ",
                Lat = 10.7805,
                Lng = 106.6992,
                Radius = 80,
                AudioUrl = "audio/buudien.mp3"
            }
        };

        // 🔹 Lấy tất cả
        public List<POI> GetAll()
        {
            return _data;
        }

        // 🔹 Lấy theo ID
        public POI? GetById(int id)
        {
            return _data.FirstOrDefault(x => x.Id == id);
        }

        // 🔹 Thêm mới
        public POI Add(POI poi)
        {
            poi.Id = _data.Count > 0 ? _data.Max(x => x.Id) + 1 : 1;
            _data.Add(poi);
            return poi;
        }

        // 🔹 Cập nhật
        public bool Update(int id, POI updated)
        {
            var poi = _data.FirstOrDefault(x => x.Id == id);
            if (poi == null) return false;

            poi.Name = updated.Name;
            poi.Description = updated.Description;
            poi.Lat = updated.Lat;
            poi.Lng = updated.Lng;
            poi.Radius = updated.Radius;
            poi.AudioUrl = updated.AudioUrl;

            return true;
        }

        // 🔹 Xóa
        public bool Delete(int id)
        {
            var poi = _data.FirstOrDefault(x => x.Id == id);
            if (poi == null) return false;

            _data.Remove(poi);
            return true;
        }
    }
}
using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Services;

public interface IDatabaseService
{
    /// <summary>
    /// Lấy tất cả các POI từ database
    /// </summary>
    Task<List<Poi>> GetAllPoisAsync();

    /// <summary>
    /// Đồng bộ dữ liệu POI từ API xuống database cục bộ
    /// </summary>
    Task SyncPoisFromApiAsync(IApiService apiService);

    /// <summary>
    /// (Tùy chọn) Lấy một POI theo ID - hữu ích cho các chức năng khác sau này
    /// </summary>
    Task<Poi?> GetPoiByIdAsync(int id);

    /// <summary>
    /// (Tùy chọn) Tìm kiếm POI theo từ khóa
    /// </summary>
    Task<List<Poi>> SearchPoisAsync(string keyword);
}
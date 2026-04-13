using Microsoft.EntityFrameworkCore;
using TourGuideHCM.App.Data;
using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Services;

public class DatabaseService : IDatabaseService
{
    private readonly AppDbContext _db;

    public DatabaseService()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "tourguide.db");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        _db = new AppDbContext(options);

        // Tạo DB nếu chưa có
        _db.Database.EnsureCreated();
    }

    // ✅ Lấy toàn bộ POI từ local DB
    public async Task<List<Poi>> GetAllPoisAsync()
    {
        return await _db.Pois.ToListAsync();
    }

    // ✅ Đồng bộ dữ liệu từ API về SQLite
    public async Task SyncPoisFromApiAsync(IApiService apiService)
    {
        try
        {
            var remotePois = await apiService.GetAllPoisAsync();

            if (remotePois == null || remotePois.Count == 0)
                return;

            // Xóa dữ liệu cũ
            _db.Pois.RemoveRange(_db.Pois);

            // Thêm dữ liệu mới
            await _db.Pois.AddRangeAsync(remotePois);

            await _db.SaveChangesAsync();

            Console.WriteLine($"Đã đồng bộ {remotePois.Count} POI từ API");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi sync POI: {ex.Message}");
        }
    }
    // ✅ Lấy POI theo ID
    public async Task<Poi?> GetPoiByIdAsync(int id)
    {
        return await _db.Pois.FirstOrDefaultAsync(p => p.Id == id);
    }

    // ✅ Tìm kiếm POI theo từ khóa
    public async Task<List<Poi>> SearchPoisAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllPoisAsync();

        var lower = keyword.ToLower();
        return await _db.Pois
            .Where(p => p.Name.ToLower().Contains(lower) ||
                        p.Description.ToLower().Contains(lower))
            .ToListAsync();
    }
}
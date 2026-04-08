using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Services;

public interface IDatabaseService
{
    Task<List<Poi>> GetAllPoisAsync();
    Task SyncPoisFromApiAsync(IApiService apiService);
}
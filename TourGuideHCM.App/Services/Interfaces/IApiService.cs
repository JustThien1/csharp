using Refit;
using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Services;

public interface IApiService
{
    [Get("/api/poi")]
    Task<List<Poi>> GetAllPoisAsync();

    [Get("/api/poi/nearby")]
    Task<Poi?> GetNearbyAsync(double lat, double lng);
}
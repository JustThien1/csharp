using Refit;
using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Services;

public class ApiService : IApiService
{
    private readonly IApiService _refitClient;

    public ApiService()
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5284")   // Port của API bạn
        };

        _refitClient = RestService.For<IApiService>(httpClient);
    }

    public Task<List<Poi>> GetAllPoisAsync() => _refitClient.GetAllPoisAsync();

    public Task<Poi?> GetNearbyAsync(double lat, double lng) => _refitClient.GetNearbyAsync(lat, lng);
}
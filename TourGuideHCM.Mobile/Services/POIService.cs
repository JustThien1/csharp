using System.Net.Http.Json;
using TourGuideHCM.Mobile.Models;

namespace TourGuideHCM.Mobile.Services;

public class POIService
{
    private readonly HttpClient _httpClient;

    public POIService()
    {
        var baseUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5284/api"      // Android Emulator
            : "http://localhost:5284/api";     // Windows / iOS

        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<List<POI>> GetAllAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/poi");
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<POI>>() ?? new List<POI>()
                : new List<POI>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetAll POI error: {ex.Message}");
            return new List<POI>();
        }
    }
}
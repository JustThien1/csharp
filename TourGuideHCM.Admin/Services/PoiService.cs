using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services;

public class PoiService
{
    private readonly HttpClient _http;

    public PoiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Poi>> GetAll()
    {
        return await _http.GetFromJsonAsync<List<Poi>>("api/poi") ?? new();
    }

    public async Task Create(Poi poi)
    {
        await _http.PostAsJsonAsync("api/poi", poi);
    }

    public async Task Update(Poi poi)
    {
        await _http.PutAsJsonAsync($"api/poi/{poi.Id}", poi);
    }

    public async Task Delete(int id)
    {
        await _http.DeleteAsync($"api/poi/{id}");
    }
}
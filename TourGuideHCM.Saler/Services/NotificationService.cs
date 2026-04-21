using System.Net.Http.Json;
using TourGuideHCM.Saler.Models;

namespace TourGuideHCM.Saler.Services;

public class NotificationService
{
    private readonly HttpClient _http;

    public NotificationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<NotificationDto>> GetMineAsync(int limit = 50)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<NotificationDto>>(
                $"api/notification?limit={limit}") ?? new();
        }
        catch { return new(); }
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            var res = await _http.GetFromJsonAsync<CountResponse>("api/notification/count");
            return res?.Count ?? 0;
        }
        catch { return 0; }
    }

    public async Task<bool> MarkReadAsync(int id)
    {
        try
        {
            var res = await _http.PostAsync($"api/notification/{id}/mark-read", null);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> MarkAllReadAsync()
    {
        try
        {
            var res = await _http.PostAsync("api/notification/mark-all-read", null);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private class CountResponse { public int Count { get; set; } }
}

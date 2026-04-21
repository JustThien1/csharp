using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services;

public class PlaybackService
{
    private readonly HttpClient _http;

    public PlaybackService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Dùng endpoint /table vì nó trả đầy đủ DeviceName + Platform (thay cho User cũ).
    /// Endpoint /history cũ chỉ có UserId → không hữu ích cho app ẩn danh.
    /// </summary>
    public async Task<List<PlaybackHistoryDto>> GetHistoryAsync(int limit = 100, int days = 0)
    {
        try
        {
            var url = days > 0
                ? $"api/playback/table?limit={limit}&days={days}"
                : $"api/playback/table?limit={limit}";

            return await _http.GetFromJsonAsync<List<PlaybackHistoryDto>>(url)
                   ?? new List<PlaybackHistoryDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PlaybackService] GetHistory Error: {ex.Message}");
            return new List<PlaybackHistoryDto>();
        }
    }
}

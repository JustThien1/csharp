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

    public async Task<List<PlaybackHistoryDto>> GetHistoryAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<PlaybackHistoryDto>>("api/playback/history")
                   ?? new List<PlaybackHistoryDto>();
        }
        catch
        {
            return new List<PlaybackHistoryDto>();
        }
    }
}
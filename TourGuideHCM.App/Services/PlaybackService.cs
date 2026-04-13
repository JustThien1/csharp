using System.Net.Http.Json;

namespace TourGuideHCM.App.Services;

public class PlaybackService
{
    private readonly HttpClient _http;

    public PlaybackService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("http://localhost:5284/");
    }

    public async Task LogPlayback(int userId, int poiId, int durationSeconds = 0)
    {
        if (userId <= 0 || poiId <= 0) return;

        try
        {
            var dto = new
            {
                userId,
                poiId,
                durationSeconds,
                triggerType = "app"
            };

            await _http.PostAsJsonAsync("api/playback", dto);
            Console.WriteLine($"✅ Logged: User {userId} - POI {poiId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ LogPlayback error: {ex.Message}");
        }
    }
}
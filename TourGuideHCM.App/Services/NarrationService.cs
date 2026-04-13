using Microsoft.Maui.Media;
using System.Net.Http.Json;
using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Services;

public interface INarrationService
{
    Task PlayNarrationForPoi(string poiIdentifier);
    Task Speak(string text);
}

public class NarrationService : INarrationService
{
    private readonly IDatabaseService _databaseService;
    private readonly HttpClient _http;

    public NarrationService(IDatabaseService databaseService, HttpClient http)
    {
        _databaseService = databaseService;
        _http = http;
    }

    public async Task PlayNarrationForPoi(string poiIdentifier)
    {
        try
        {
            if (!int.TryParse(poiIdentifier, out int poiId))
                return;

            var pois = await _databaseService.GetAllPoisAsync();
            var poi = pois.FirstOrDefault(p => p.Id == poiId);

            if (poi == null) return;

            string textToSpeak = !string.IsNullOrEmpty(poi.NarrationText)
                ? poi.NarrationText
                : $"Bạn đang gần {poi.Name}";

            var startTime = DateTime.UtcNow;

            await Speak(textToSpeak);

            // ✅ Tính thời gian phát và ghi log lên API
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            await LogPlaybackAsync(poiId, duration, "geofence");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Narration ERROR: " + ex.Message);
        }
    }

    public async Task Speak(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var locales = await TextToSpeech.Default.GetLocalesAsync();

            var vietnamese = locales.FirstOrDefault(l =>
                (l.Language != null && l.Language.Contains("vi")) ||
                (l.Country != null && l.Country.Contains("VN")))
                ?? locales.FirstOrDefault();

            await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
            {
                Locale = vietnamese
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("TTS ERROR: " + ex.Message);
        }
    }

    // ✅ Ghi log lên API server
    private async Task LogPlaybackAsync(int poiId, double duration, string triggerType)
    {
        try
        {
            // Lấy username hiện tại nếu đã login
            var username = Preferences.Get("username", null);

            var payload = new
            {
                POIId = poiId,
                DurationSeconds = duration,
                TriggerType = triggerType,
                TriggeredAt = DateTime.UtcNow
            };

            var response = await _http.PostAsJsonAsync("/api/playback", payload);

            if (response.IsSuccessStatusCode)
                Console.WriteLine($"✅ Đã lưu playback log: POI {poiId}, {duration:F1}s");
            else
                Console.WriteLine($"⚠️ Lưu playback log thất bại: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            // Không throw — lỗi log không được ảnh hưởng UX
            Console.WriteLine($"❌ LogPlayback ERROR: {ex.Message}");
        }
    }
}
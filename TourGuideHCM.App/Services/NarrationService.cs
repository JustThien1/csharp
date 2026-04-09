using Microsoft.Maui.Media;
using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Services;

public interface INarrationService
{
    Task PlayNarrationForPoi(string poiIdentifier);
    Task Speak(string text); // 🔥 thêm để dùng chung
}

public class NarrationService : INarrationService
{
    private readonly IDatabaseService _databaseService;

    public NarrationService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    // 🔊 đọc theo POI (chuẩn đồ án)
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

            await Speak(textToSpeak);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Narration ERROR: " + ex.Message);
        }
    }

    // 🔥 dùng chung (MapViewModel gọi)
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
}
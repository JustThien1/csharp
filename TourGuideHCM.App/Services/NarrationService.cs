using TourGuideHCM.App.Models;
using TourGuideHCM.App.Data;

namespace TourGuideHCM.App.Services;

public interface INarrationService
{
    Task PlayNarrationForPoi(string poiIdentifier);
}

public class NarrationService : INarrationService
{
    private readonly IDatabaseService _databaseService;

    public NarrationService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task PlayNarrationForPoi(string poiIdentifier)
    {
        if (!int.TryParse(poiIdentifier, out int poiId)) return;

        var pois = await _databaseService.GetAllPoisAsync();
        var poi = pois.FirstOrDefault(p => p.Id == poiId);

        if (poi == null) return;

        string textToSpeak = !string.IsNullOrEmpty(poi.NarrationText)
            ? poi.NarrationText
            : $"Bạn đang gần {poi.Name}";

        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var vietnamese = locales.FirstOrDefault(l => l.Language.Contains("vi") || l.Country.Contains("VN"))
                         ?? locales.FirstOrDefault();

        await TextToSpeech.Default.SpeakAsync(textToSpeak, new SpeechOptions
        {
            Locale = vietnamese
        });
    }
}
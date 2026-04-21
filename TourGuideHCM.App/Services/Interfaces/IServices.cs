// IServices.cs - ĐÃ BỎ TOÀN BỘ AUTH
using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Services.Interfaces;

// ── IApiService ───────────────────────────────────────────────────────────────
public interface IApiService
{
    // POI
    Task<List<POI>> GetPoisAsync();
    Task<POI?> GetPoiByIdAsync(int id);
    Task<POI?> GetNearbyPoiAsync(double lat, double lng);

    // Geofence
    Task<GeofenceTriggerResponse?> TriggerGeofenceAsync(double lat, double lng);

    // Playback & Route Log — ĐÃ MỞ RỘNG cho monitoring
    Task LogPlaybackAsync(int userId, int poiId, string triggerType, int? durationSeconds = null,
                          string? deviceId = null, string? deviceName = null, string? platform = null);
    Task LogRouteAsync(int userId, double lat, double lng, string? deviceId = null);

    // Monitoring Heartbeat — MỚI
    Task SendHeartbeatAsync(int userId, string deviceId, string deviceName, string platform);

    // Audio
    string ResolveAudioUrl(string? audioUrl);
    Task<string> GetAudioUrlAsync(int poiId, string language = "vi");
}

// ── IDatabaseService ──────────────────────────────────────────────────────────
public interface IDatabaseService
{
    Task InitAsync();
    Task<List<POI>> GetCachedPoisAsync();
    Task UpsertPoisAsync(IEnumerable<POI> pois);
    Task<POI?> GetPoiByIdAsync(int id);
    Task SaveUserAsync(User user);           // vẫn giữ tạm (dùng cho local cache)
    Task<User?> GetCurrentUserAsync();
    Task ClearUserAsync();
    Task AddPlaybackHistoryAsync(PlaybackHistory history);
    Task<List<PlaybackHistory>> GetPlaybackHistoryAsync(int limit = 50);
    Task AddGeofenceEventAsync(GeofenceEvent evt);
    Task ClearPoisAsync();
}

// ── IGeofenceService ──────────────────────────────────────────────────────────
public interface IGeofenceService
{
    event EventHandler<GeofenceTriggeredEventArgs>? GeofenceTriggered;
    event EventHandler<LocationUpdate>? LocationUpdated;
    event EventHandler<TourGuideHCM.App.Services.PoisInRangeEventArgs>? PoisInRangeChanged;

    bool IsRunning { get; }
    Task StartAsync(IEnumerable<POI> pois);
    Task StopAsync();
    Task UpdatePoisAsync(IEnumerable<POI> pois);
    double CalculateDistance(double lat1, double lng1, double lat2, double lng2);
}

public class GeofenceTriggeredEventArgs : EventArgs
{
    public POI Poi { get; init; } = null!;
    public string TriggerType { get; init; } = string.Empty;
    public double Distance { get; init; }
}

// ── INarrationService ─────────────────────────────────────────────────────────
public interface INarrationService
{
    bool IsSpeaking { get; }
    bool IsPlayingAudio { get; }
    event EventHandler? NarrationStarted;
    event EventHandler? NarrationCompleted;
    Task PlayAsync(NarrationRequest request);
    Task StopAsync();
}

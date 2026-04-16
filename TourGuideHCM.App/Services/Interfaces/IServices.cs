using TourGuideHCM.App.Models;

// File này thay thế toàn bộ 4 file interface cũ:
//   IApiService.cs     → XÓA (dùng Refit, GetAllPoisAsync - sai)
//   IDatabaseService.cs → XÓA (GetAllPoisAsync, SyncPoisFromApiAsync - sai)
//   IGeofenceService.cs → XÓA (dùng Shiny.Locations - sai)
//   INarrationService.cs → XÓA (chỉ có Task Speak(string) - thiếu)

namespace TourGuideHCM.App.Services.Interfaces;

// ── IApiService ───────────────────────────────────────────────────────────────
public interface IApiService
{
    // POI
    Task<List<POI>> GetPoisAsync();
    Task<POI?> GetPoiByIdAsync(int id);
    Task<POI?> GetNearbyPoiAsync(double lat, double lng);

    // Auth
    Task<LoginResponse?> LoginAsync(string username, string password);
    Task<RegisterResponse?> RegisterAsync(string username, string password, string fullName, string email);

    // Geofence – POST /api/poi/trigger
    Task<GeofenceTriggerResponse?> TriggerGeofenceAsync(double lat, double lng);

    // Playback – POST /api/playback
    Task LogPlaybackAsync(int userId, int poiId, string triggerType, int? durationSeconds = null);

    // Audio URL resolver
    string ResolveAudioUrl(string? audioUrl);
    Task<string> GetAudioUrlAsync(int poiId, string language = "vi");

    // Route log
    Task LogRouteAsync(int userId, double lat, double lng);
}

// ── IDatabaseService ──────────────────────────────────────────────────────────
public interface IDatabaseService
{
    Task InitAsync();
    Task<List<POI>> GetCachedPoisAsync();
    Task UpsertPoisAsync(IEnumerable<POI> pois);
    Task<POI?> GetPoiByIdAsync(int id);
    Task SaveUserAsync(User user);
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

// ── IAuthService ──────────────────────────────────────────────────────────────
public interface IAuthService
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    Task<bool> LoginAsync(string username, string password);
    Task<bool> RegisterAsync(string username, string password, string fullName, string email);
    Task LogoutAsync();
    Task<bool> TryAutoLoginAsync();
}
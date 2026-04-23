using System.Net.Http.Json;
using System.Text.Json;
using TourGuideHCM.App.Helpers;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(IHttpClientFactory factory)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _http = factory.CreateClient("DefaultClient");
    }

    // ── POI ──────────────────────────────────────────────────────────────────
    public async Task<List<POI>> GetPoisAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<POI>>("/api/poi", _json) ?? new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetPois: {ex.Message}");
            return new();
        }
    }

    public async Task<POI?> GetPoiByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<POI>($"/api/poi/{id}", _json);
        }
        catch { return null; }
    }

    public async Task<POI?> GetNearbyPoiAsync(double lat, double lng)
    {
        try
        {
            return await _http.GetFromJsonAsync<POI>(
                $"/api/poi/nearby?lat={lat}&lng={lng}", _json);
        }
        catch { return null; }
    }

    // ── Geofence Trigger ─────────────────────────────────────────────────────
    public async Task<GeofenceTriggerResponse?> TriggerGeofenceAsync(double lat, double lng)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/poi/trigger",
                new GeofenceTriggerRequest { Lat = lat, Lng = lng });

            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<GeofenceTriggerResponse>(_json);
        }
        catch { return null; }
    }

    // ── Playback Log ─────────────────────────────────────────────────────────
    // ĐÃ MỞ RỘNG: nhận thêm deviceId/deviceName/platform để backend phân biệt thiết bị
    public async Task LogPlaybackAsync(int userId, int poiId, string triggerType, int? durationSeconds = null,
                                       string? deviceId = null, string? deviceName = null, string? platform = null)
    {
        try
        {
            var payload = new
            {
                UserId = userId,
                POIId = poiId,
                TriggerType = triggerType,
                DurationSeconds = durationSeconds,
                DeviceId = deviceId,
                DeviceName = deviceName,
                Platform = platform
            };

            var response = await _http.PostAsJsonAsync("/api/analytics/playback", payload);
            Console.WriteLine($"📤 LogPlayback → POI={poiId}, Device={deviceId?[..8]}, Status={(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ LogPlayback Error: {ex.Message}");
        }
    }

    // ── Heartbeat (MỚI - cho Monitoring) ─────────────────────────────────────
    public async Task SendHeartbeatAsync(int userId, string deviceId, string deviceName, string platform)
    {
        try
        {
            var heartbeatUrl = new Uri(_http.BaseAddress!, "/api/analytics/heartbeat");
            var payload = new
            {
                UserId = userId,
                DeviceId = deviceId,
                DeviceName = deviceName,
                Platform = platform
            };

            Console.WriteLine($"💓 Sending heartbeat → {heartbeatUrl} | Device={deviceId[..Math.Min(8, deviceId.Length)]} | UserId={userId}");
            var response = await _http.PostAsJsonAsync("/api/analytics/heartbeat", payload);
            Console.WriteLine($"💓 Heartbeat response ← {(int)response.StatusCode} {response.ReasonPhrase}");
            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"💔 Heartbeat status: {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Heartbeat Error: {ex.Message}");
        }
    }

    public async Task LogRouteAsync(int userId, double lat, double lng, string? deviceId = null)
    {
        try
        {
            await _http.PostAsJsonAsync("/api/route/log", new
            {
                userId = userId > 0 ? (int?)userId : null,
                lat,
                lng,
                deviceId
            });
        }
        catch { }
    }

    // ── Audio ────────────────────────────────────────────────────────────────
    public string ResolveAudioUrl(string? audioUrl)
    {
        if (string.IsNullOrEmpty(audioUrl)) return string.Empty;
        if (audioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return audioUrl;

        return $"{DeviceHelper.GetBaseUrl().TrimEnd('/')}/{audioUrl.TrimStart('/')}";
    }

    public async Task<string> GetAudioUrlAsync(int poiId, string language = "vi")
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<AudioItem>>(
                $"/api/audio/poi/{poiId}", _json) ?? new();

            var match = list.FirstOrDefault(a => a.IsActive && a.Language == language)
                     ?? list.FirstOrDefault(a => a.IsActive);

            return match?.AudioUrl != null ? ResolveAudioUrl(match.AudioUrl) : string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] GetAudioUrl: {ex.Message}");
            return string.Empty;
        }
    }

    private class AudioItem
    {
        public int Id { get; set; }
        public string Language { get; set; } = "";
        public string? AudioUrl { get; set; }
        public bool IsActive { get; set; }
    }
}

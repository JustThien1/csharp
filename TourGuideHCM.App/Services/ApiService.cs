using System.Net.Http.Json;
using System.Text.Json;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _http;

    // ── Base URL – đổi theo môi trường ──────────────────────────────────────
    // Android emulator  → http://10.0.2.2:5284
    // iOS simulator     → http://localhost:5284
    // Thiết bị thật     → http://<IP-LAN>:5284
#if ANDROID
    public const string BaseUrl = "http://10.0.2.2:5284";
#else
    public const string BaseUrl = "http://localhost:5284";
#endif

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
        };
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    // ── POI ──────────────────────────────────────────────────────────────────

    /// <summary>GET /api/poi</summary>
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

    /// <summary>GET /api/poi/{id}</summary>
    public async Task<POI?> GetPoiByIdAsync(int id)
    {
        try { return await _http.GetFromJsonAsync<POI>($"/api/poi/{id}", _json); }
        catch { return null; }
    }

    /// <summary>GET /api/poi/nearby?lat=...&lng=...</summary>
    public async Task<POI?> GetNearbyPoiAsync(double lat, double lng)
    {
        try
        {
            return await _http.GetFromJsonAsync<POI>(
                $"/api/poi/nearby?lat={lat}&lng={lng}", _json);
        }
        catch { return null; }
    }

    // ── Auth ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/auth/login
    /// Body: { "username":"...", "passwordHash":"..." }
    /// API so sánh plain text – KHÔNG hash
    /// Response: { "message":"...", "userId":1, "username":"..." }
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/auth/login",
                new LoginRequest { Username = username, PasswordHash = password });

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[API] Login failed: {err}");
                return null;
            }
            return await resp.Content.ReadFromJsonAsync<LoginResponse>(_json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] Login: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// POST /api/auth/register
    /// Body: { "username":"...", "passwordHash":"...", "fullName":"...", "email":"..." }
    /// Response: { "message":"...", "userId":1 }
    /// </summary>
    public async Task<RegisterResponse?> RegisterAsync(
        string username, string password, string fullName, string email)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("/api/auth/register",
                new RegisterRequest
                {
                    Username = username,
                    PasswordHash = password,  // API lưu plain text
                    FullName = fullName,
                    Email = email
                });

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[API] Register failed: {err}");
                return null;
            }
            return await resp.Content.ReadFromJsonAsync<RegisterResponse>(_json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] Register: {ex.Message}");
            return null;
        }
    }

    // ── Geofence ──────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/poi/trigger
    /// Body: { "lat":..., "lng":... }
    /// API dùng GetCurrentUserId() từ JWT claim → nếu chưa auth thì userId=0 (vẫn OK)
    /// Response: { "triggered":true, "poiId":1, "poiName":"...", "audioUrl":"...", "narrationText":"..." }
    /// </summary>
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

    // ── Playback Log ──────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/playback
    /// Body: { "userId":1, "poiId":1, "durationSeconds":null, "triggerType":"manual" }
    /// </summary>
    public async Task LogPlaybackAsync(int userId, int poiId, string triggerType,
        int? durationSeconds = null)
    {
        try
        {
            await _http.PostAsJsonAsync("/api/playback",
                new PlaybackLogRequest
                {
                    UserId = userId,
                    POIId = poiId,
                    TriggerType = triggerType,
                    DurationSeconds = durationSeconds
                });
        }
        catch { /* fire-and-forget */ }
    }

    // ── Audio URL ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Chuyển relative path → URL đầy đủ để stream từ API
    /// "audio/nhatho.mp3" → "http://10.0.2.2:5284/audio/nhatho.mp3"
    /// </summary>
    public string ResolveAudioUrl(string? audioUrl)
    {
        if (string.IsNullOrEmpty(audioUrl)) return string.Empty;
        if (audioUrl.StartsWith("http")) return audioUrl;
        return $"{BaseUrl}/{audioUrl.TrimStart('/')}";
    }

    // ── Audio URL từ bảng Audios ─────────────────────────────────────────────────

    /// <summary>
    /// GET /api/audio/poi/{poiId} → lấy AudioUrl đúng ngôn ngữ từ bảng Audios
    /// </summary>
    public async Task<string> GetAudioUrlAsync(int poiId, string language = "vi")
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<AudioItem>>(
                $"/api/audio/poi/{poiId}", _json) ?? new();

            // Ưu tiên đúng ngôn ngữ, fallback về bất kỳ
            var match = list.FirstOrDefault(a =>
                a.IsActive && a.Language == language)
                ?? list.FirstOrDefault(a => a.IsActive);

            if (match == null) return string.Empty;

            var url = match.AudioUrl ?? string.Empty;
#if ANDROID
            url = url.Replace("localhost", "10.0.2.2")
                     .Replace("127.0.0.1", "10.0.2.2");
#endif
            return ResolveAudioUrl(url);
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

    // ── Route Log ─────────────────────────────────────────────────────────────

    /// <summary>POST /api/route/log — Lưu vị trí để tạo tuyến di chuyển</summary>
    public async Task LogRouteAsync(int userId, double lat, double lng)
    {
        try
        {
            if (userId <= 0) return;
            await _http.PostAsJsonAsync("/api/route/log", new { userId, lat, lng });
        }
        catch { /* fire-and-forget */ }
    }
}
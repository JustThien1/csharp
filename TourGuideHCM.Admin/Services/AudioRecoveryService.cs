using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services;

public class AudioRecoveryService
{
    private readonly HttpClient _http;

    public AudioRecoveryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<OrphanFileDto>> GetOrphansAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<OrphanFileDto>>(
                "api/audio-recovery/orphans") ?? new();
        }
        catch { return new(); }
    }

    public async Task<(bool ok, string message)> RestoreAsync(
        string fileName, int poiId, string language, int? durationSeconds = null)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/audio-recovery/restore", new
            {
                fileName,
                poiId,
                language,
                durationSeconds
            });

            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<RestoreResponse>();
                return (true, body?.Message ?? "Đã gán thành công");
            }

            var errBody = await res.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, errBody?.Message ?? $"HTTP {(int)res.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<bool> DeleteOrphanAsync(string fileName)
    {
        try
        {
            var res = await _http.DeleteAsync(
                $"api/audio-recovery/orphans/{Uri.EscapeDataString(fileName)}");
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private class RestoreResponse { public string? Message { get; set; } }
    private class ErrorResponse { public string? Message { get; set; } }
}

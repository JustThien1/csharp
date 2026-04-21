using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services;

public class TtsQueueService
{
    private readonly HttpClient _http;

    public TtsQueueService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TtsJobDto>> GetQueueAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<TtsJobDto>>("api/tts/queue")
                   ?? new List<TtsJobDto>();
        }
        catch { return new List<TtsJobDto>(); }
    }

    public async Task<bool> RetryJobAsync(int jobId)
    {
        try
        {
            var response = await _http.PostAsync($"api/tts/retry/{jobId}", null);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> CancelJobAsync(int jobId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/tts/cancel/{jobId}");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ClearFailedAsync()
    {
        try
        {
            var response = await _http.DeleteAsync("api/tts/clear-failed");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
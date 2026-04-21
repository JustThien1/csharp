using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services;

public class POIApprovalService
{
    private readonly HttpClient _http;

    public POIApprovalService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ApprovalItemDto>> GetListAsync(string status = "PendingReview")
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ApprovalItemDto>>(
                $"api/poi-approval?status={status}") ?? new();
        }
        catch { return new(); }
    }

    public async Task<int> GetPendingCountAsync()
    {
        try
        {
            var res = await _http.GetFromJsonAsync<CountResponse>("api/poi-approval/pending-count");
            return res?.Count ?? 0;
        }
        catch { return 0; }
    }

    public async Task<(bool ok, string message)> ApproveAsync(int id)
    {
        try
        {
            var res = await _http.PostAsync($"api/poi-approval/{id}/approve", null);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, err?.Message ?? "Thao tác thất bại");
            }
            var body = await res.Content.ReadFromJsonAsync<MessageResponse>();
            return (true, body?.Message ?? "OK");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string message)> RejectAsync(int id, string reason)
    {
        try
        {
            var res = await _http.PostAsJsonAsync($"api/poi-approval/{id}/reject", new { reason });
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, err?.Message ?? "Thao tác thất bại");
            }
            var body = await res.Content.ReadFromJsonAsync<MessageResponse>();
            return (true, body?.Message ?? "OK");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string message)> LockAsync(int id, string reason)
    {
        try
        {
            var res = await _http.PostAsJsonAsync($"api/poi-approval/{id}/lock", new { reason });
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, err?.Message ?? "Thao tác thất bại");
            }
            var body = await res.Content.ReadFromJsonAsync<MessageResponse>();
            return (true, body?.Message ?? "OK");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string message)> UnlockAsync(int id)
    {
        try
        {
            var res = await _http.PostAsync($"api/poi-approval/{id}/unlock", null);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, err?.Message ?? "Thao tác thất bại");
            }
            var body = await res.Content.ReadFromJsonAsync<MessageResponse>();
            return (true, body?.Message ?? "OK");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    private class CountResponse { public int Count { get; set; } }
    private class MessageResponse { public string? Message { get; set; } }
    private class ErrorResponse { public string? Message { get; set; } }
}

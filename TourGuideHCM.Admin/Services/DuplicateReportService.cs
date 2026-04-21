using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services;

public class DuplicateReportService
{
    private readonly HttpClient _http;

    public DuplicateReportService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<DuplicateReportDto>> GetAllAsync(string status = "Open")
    {
        try
        {
            return await _http.GetFromJsonAsync<List<DuplicateReportDto>>(
                $"api/duplicate-reports?status={status}") ?? new();
        }
        catch { return new(); }
    }

    public async Task<int> GetOpenCountAsync()
    {
        try
        {
            var res = await _http.GetFromJsonAsync<CountResponse>("api/duplicate-reports/count");
            return res?.Count ?? 0;
        }
        catch { return 0; }
    }

    public async Task<(bool ok, string message, int newReports)> ScanAllAsync()
    {
        try
        {
            var res = await _http.PostAsync("api/duplicate-reports/scan", null);
            if (!res.IsSuccessStatusCode)
                return (false, "Scan thất bại", 0);

            var body = await res.Content.ReadFromJsonAsync<ScanResponse>();
            return (true, body?.Message ?? "Scan xong", body?.NewReports ?? 0);
        }
        catch (Exception ex) { return (false, ex.Message, 0); }
    }

    public async Task<bool> KeepBothAsync(int reportId, string? note = null, string? adminName = null)
    {
        try
        {
            var res = await _http.PostAsJsonAsync(
                $"api/duplicate-reports/{reportId}/keep-both",
                new { note, adminName });
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> MergeAsync(int reportId, int keepId, string? note = null, string? adminName = null)
    {
        try
        {
            var res = await _http.PostAsJsonAsync(
                $"api/duplicate-reports/{reportId}/merge",
                new { keepId, note, adminName });
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> DismissAsync(int reportId, string? note = null, string? adminName = null)
    {
        try
        {
            var res = await _http.PostAsJsonAsync(
                $"api/duplicate-reports/{reportId}/dismiss",
                new { note, adminName });
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private class CountResponse { public int Count { get; set; } }
    private class ScanResponse { public string? Message { get; set; } public int NewReports { get; set; } }
}

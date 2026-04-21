using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services;

public class AnalyticsService
{
    private readonly HttpClient _http;

    public AnalyticsService(HttpClient http)
    {
        _http = http;
    }

    // Dùng cho Dashboard Analytics — gọi endpoint /overview (không phải /dashboard vì endpoint đó đã đổi thành realtime)
    public async Task<DashboardDto?> GetDashboardAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<DashboardDto>("api/analytics/overview");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnalyticsService] GetDashboard Error: {ex.Message}");
            return null;
        }
    }

    // Dùng cho Monitoring Realtime
    public async Task<RealtimeDashboardDto?> GetRealtimeDashboardAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<RealtimeDashboardDto>("api/analytics/realtime");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnalyticsService] GetRealtimeDashboard Error: {ex.Message}");
            return null;
        }
    }

    // Tạo dữ liệu realtime demo (Monitoring) — thiết bị online tức thời
    public async Task<bool> SeedDemoDataAsync()
    {
        try
        {
            var res = await _http.PostAsync("api/analytics/seed-demo", null);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnalyticsService] SeedDemo Error: {ex.Message}");
            return false;
        }
    }

    // MỚI: Tạo dữ liệu demo cho Dashboard — rải 150 lượt nghe trong 7 ngày qua
    public async Task<bool> SeedDashboardDemoAsync()
    {
        try
        {
            var res = await _http.PostAsync("api/analytics/seed-dashboard-demo", null);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnalyticsService] SeedDashboardDemo Error: {ex.Message}");
            return false;
        }
    }

    // Method cũ để tránh lỗi (nếu còn component nào gọi)
    public async Task<DashboardDto?> GetDashboard()
    {
        return await GetDashboardAsync();
    }
}

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

    // Method cũ để tránh lỗi (nếu còn component nào gọi)
    public async Task<DashboardDto?> GetDashboard()
    {
        return await GetDashboardAsync();
    }
}

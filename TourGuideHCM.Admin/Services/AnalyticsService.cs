using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services
{
    public class AnalyticsService
    {
        private readonly HttpClient _http;

        public AnalyticsService(HttpClient http)
        {
            _http = http;
        }

        public async Task<DashboardDto> GetDashboard()
        {
            return await _http.GetFromJsonAsync<DashboardDto>("api/analytics/dashboard")
                ?? new DashboardDto(); // 👈 FIX;
        }
    }
}
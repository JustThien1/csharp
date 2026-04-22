using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services;

public class PaymentHistoryService
{
    private readonly HttpClient _http;

    public PaymentHistoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PaymentHistoryPageDto> GetAsync(
        int page,
        int pageSize,
        int? userId,
        string? username,
        string? status,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (userId.HasValue) query.Add($"userId={userId.Value}");
        if (!string.IsNullOrWhiteSpace(username)) query.Add($"username={Uri.EscapeDataString(username.Trim())}");
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status.Trim())}");
        if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) query.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        return await _http.GetFromJsonAsync<PaymentHistoryPageDto>($"api/payments/admin/history?{string.Join("&", query)}")
               ?? new PaymentHistoryPageDto();
    }
}

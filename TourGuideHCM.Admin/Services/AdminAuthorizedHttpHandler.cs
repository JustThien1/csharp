using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace TourGuideHCM.Admin.Services;

/// <summary>
/// Gắn Bearer token vào mỗi HTTP request đi qua HttpClient "API".
/// Token được lấy từ localStorage (lưu bởi AdminAuthService).
/// </summary>
public class AdminAuthorizedHttpHandler : DelegatingHandler
{
    private readonly ILocalStorageService _storage;
    private const string TokenKey = "admin_jwt";

    public AdminAuthorizedHttpHandler(ILocalStorageService storage)
    {
        _storage = storage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _storage.GetItemAsync<string>(TokenKey, cancellationToken);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch { /* localStorage chưa sẵn sàng → bỏ qua */ }

        return await base.SendAsync(request, cancellationToken);
    }
}

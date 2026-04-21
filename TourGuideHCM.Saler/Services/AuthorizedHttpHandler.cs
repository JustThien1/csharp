using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace TourGuideHCM.Saler.Services;

/// <summary>
/// HttpMessageHandler tự động gắn Bearer token vào Authorization header
/// của mỗi request đi qua named HttpClient "API".
/// </summary>
public class AuthorizedHttpHandler : DelegatingHandler
{
    private readonly ILocalStorageService _storage;
    private const string TokenKey = "saler_jwt";

    public AuthorizedHttpHandler(ILocalStorageService storage)
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
        catch
        {
            // localStorage có thể không sẵn sàng khi app vừa khởi động → bỏ qua
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

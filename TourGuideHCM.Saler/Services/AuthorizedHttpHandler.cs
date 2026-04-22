using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using TourGuideHCM.Saler.Models;

namespace TourGuideHCM.Saler.Services;

/// <summary>
/// HttpMessageHandler tự động gắn Bearer token vào Authorization header
/// của mỗi request đi qua named HttpClient "API".
/// </summary>
public class AuthorizedHttpHandler : DelegatingHandler
{
    private readonly ILocalStorageService _storage;
    private readonly NavigationManager _navigationManager;
    private readonly SubscriptionStateService _subscriptionState;
    private const string TokenKey = "saler_jwt";

    public AuthorizedHttpHandler(
        ILocalStorageService storage,
        NavigationManager navigationManager,
        SubscriptionStateService subscriptionState)
    {
        _storage = storage;
        _navigationManager = navigationManager;
        _subscriptionState = subscriptionState;
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
        }

        var response = await base.SendAsync(request, cancellationToken);

        if ((int)response.StatusCode == 402)
        {
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            ApiErrorResponse? payload = null;

            if (!string.IsNullOrWhiteSpace(rawBody))
            {
                try
                {
                    payload = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch
                {
                }

                response.Content = new StringContent(rawBody, Encoding.UTF8, "application/json");
            }

            _subscriptionState.ShowExpired(payload?.Message, payload?.SubscriptionExpiresAt);

            if (!_navigationManager.Uri.Contains("/subscription", StringComparison.OrdinalIgnoreCase) &&
                !_navigationManager.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
            {
                _navigationManager.NavigateTo("/subscription");
            }
        }

        return response;
    }
}

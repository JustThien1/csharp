using System.Net.Http.Json;
using Blazored.LocalStorage;
using TourGuideHCM.Saler.Models;

namespace TourGuideHCM.Saler.Services;

public class SubscriptionService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private readonly SubscriptionStateService _state;

    private const string UserKey = "saler_user";

    public SubscriptionService(HttpClient http, ILocalStorageService storage, SubscriptionStateService state)
    {
        _http = http;
        _storage = storage;
        _state = state;
    }

    public async Task<SubscriptionInfoResponse?> GetInfoAsync()
    {
        var data = await _http.GetFromJsonAsync<SubscriptionInfoResponse>("api/payments/subscription-info");
        if (data != null)
        {
            await UpdateStoredUserAsync(data.SubscriptionExpiresAt, data.HasActiveSubscription);
            if (data.HasActiveSubscription) _state.Clear();
        }

        return data;
    }

    public async Task<RenewalRequestResponse?> CreateRenewalRequestAsync()
    {
        var response = await _http.PostAsync("api/payments/renewal-request", null);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<RenewalRequestResponse>();
        if (data != null)
        {
            await UpdateStoredUserAsync(data.SubscriptionExpiresAt, false);
        }

        return data;
    }

    public async Task<SimulatePaymentResponse?> SimulateSuccessAsync(int paymentId)
    {
        var response = await _http.PostAsync($"api/payments/dev/simulate-success/{paymentId}", null);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<SimulatePaymentResponse>();
        if (data != null)
        {
            await UpdateStoredUserAsync(data.SubscriptionExpiresAt, true);
            _state.Clear();
        }

        return data;
    }

    private async Task UpdateStoredUserAsync(DateTime? subscriptionExpiresAt, bool hasActiveSubscription)
    {
        try
        {
            var current = await _storage.GetItemAsync<UserInfo>(UserKey);
            if (current == null) return;

            current.SubscriptionExpiresAt = subscriptionExpiresAt;
            current.HasActiveSubscription = hasActiveSubscription;
            await _storage.SetItemAsync(UserKey, current);
        }
        catch
        {
        }
    }
}

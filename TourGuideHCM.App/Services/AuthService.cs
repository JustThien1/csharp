using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

public class AuthService : IAuthService
{
    private readonly IApiService _api;
    private readonly IDatabaseService _db;
    private readonly IGeofenceService _geofence;

    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser?.IsLoggedIn == true;

    public AuthService(IApiService api, IDatabaseService db, IGeofenceService geofence)
    {
        _api = api;
        _db = db;
        _geofence = geofence;
    }

    public async Task<bool> TryAutoLoginAsync()
    {
        var user = await _db.GetCurrentUserAsync();
        if (user?.IsLoggedIn == true)
        {
            CurrentUser = user;
            return true;
        }
        return false;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var resp = await _api.LoginAsync(username, password);
        if (resp is null || resp.UserId == 0) return false;

        var user = new User
        {
            Id = resp.UserId,
            Username = resp.Username,
            IsLoggedIn = true,
            CreatedAt = DateTime.UtcNow
        };

        CurrentUser = user;
        await _db.SaveUserAsync(user);
        return true;
    }

    public async Task<bool> RegisterAsync(
        string username, string password, string fullName, string email)
    {
        var resp = await _api.RegisterAsync(username, password, fullName, email);
        if (resp is null || resp.UserId == 0) return false;

        return await LoginAsync(username, password);
    }

    public async Task LogoutAsync()
    {
        // Dừng geofence trước khi logout tránh crash
        try { await _geofence.StopAsync(); } catch { }

        CurrentUser = null;
        await _db.ClearUserAsync();
    }
}

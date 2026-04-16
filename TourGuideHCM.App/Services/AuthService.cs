using System.Net.Http.Json;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly IDatabaseService _db;

    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public AuthService(IHttpClientFactory factory, IDatabaseService db)
    {
        _http = factory.CreateClient("AuthClient");
        _db = db;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new
            {
                username,
                passwordHash = password
            });

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result == null) return false;

            CurrentUser = new User
            {
                Id = result.UserId,
                Username = result.Username,
                FullName = result.FullName,
                Phone = result.Phone
            };

            Preferences.Set("userId", result.UserId);
            Preferences.Set("username", result.Username);

            await _db.SaveUserAsync(CurrentUser);
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> RegisterAsync(string username, string password,
        string fullName, string email)
    {
        var phone = Preferences.Get("pending_phone", "");
        return await RegisterWithPhoneAsync(username, password, fullName, email, phone);
    }

    public async Task<bool> RegisterWithPhoneAsync(string username, string password,
        string? fullName, string? email, string? phone)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", new
            {
                username,
                passwordHash = password,
                fullName,
                email,
                phone
            });

            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        Preferences.Remove("userId");
        Preferences.Remove("username");
        Preferences.Remove("pending_phone");
        await _db.ClearUserAsync();
    }

    public async Task<bool> TryAutoLoginAsync()
    {
        try
        {
            var user = await _db.GetCurrentUserAsync();
            if (user == null) return false;
            CurrentUser = user;
            return true;
        }
        catch { return false; }
    }

    private class LoginResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string? FullName { get; set; }
        public string? Phone { get; set; }
    }
}
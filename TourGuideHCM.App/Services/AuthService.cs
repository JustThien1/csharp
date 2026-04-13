using System.Net.Http.Json;

namespace TourGuideHCM.App.Services;

public class AuthService
{
    private readonly HttpClient _http;

    public AuthService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5284")
        };
    }

    public async Task<int?> Login(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new
        {
            username = username,
            passwordHash = password
        });

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        return result?.UserId;
    }
    public async Task<bool> Register(string username, string password)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/register",
            new
            {
                username = username,
                passwordHash = password, // ⚠️ QUAN TRỌNG
                fullName = username,
                email = $"{username}@test.com"
            });

        return res.IsSuccessStatusCode;
    }
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
    }
}
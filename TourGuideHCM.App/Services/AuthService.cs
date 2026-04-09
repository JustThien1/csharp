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

    public async Task<bool> Login(string username, string password)
    {
        Console.WriteLine("🚀 CALL API LOGIN");

        var res = await _http.PostAsJsonAsync("/api/auth/login",
            new
            {
                username = username,
                passwordHash = password
            });

        Console.WriteLine("🚀 STATUS: " + res.StatusCode);

        return res.IsSuccessStatusCode;
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
}
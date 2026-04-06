using System.Text;
using System.Text.Json;

namespace TourGuideHCM.Mobile.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService()
    {
        _httpClient = new HttpClient
        {
            // 👉 Windows thì dùng localhost
            // 👉 Android thì đổi thành 10.0.2.2
            BaseAddress = new Uri("https://localhost:5284"),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    // ================= LOGIN =================
    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var request = new
            {
                Username = username,
                PasswordHash = password
            };

            var json = JsonSerializer.Serialize(request);

            Console.WriteLine("LOGIN REQUEST: " + json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/auth/login", content);

            var responseText = await response.Content.ReadAsStringAsync();

            Console.WriteLine("LOGIN RESPONSE: " + responseText);
            Console.WriteLine("STATUS: " + response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine("LOGIN ERROR: " + ex.Message);
            return false;
        }
    }

    // ================= REGISTER =================
    public async Task<bool> RegisterAsync(string username, string password, string fullName, string email)
    {
        try
        {
            var request = new
            {
                Username = username,
                PasswordHash = password,
                FullName = fullName,
                Email = email
            };

            var json = JsonSerializer.Serialize(request);

            Console.WriteLine("REGISTER REQUEST: " + json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/auth/register", content);

            var responseText = await response.Content.ReadAsStringAsync();

            Console.WriteLine("REGISTER RESPONSE: " + responseText);
            Console.WriteLine("STATUS: " + response.StatusCode);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine("REGISTER ERROR: " + ex.Message);
            return false;
        }
    }
}
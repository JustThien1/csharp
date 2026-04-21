using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace TourGuideHCM.Admin.Services;

/// <summary>
/// Auto-login admin khi Admin app khởi động.
/// Đơn giản cho đồ án: admin dùng credentials mặc định, không có form login.
/// 
/// Khi deploy thật, thay bằng flow đăng nhập form đầy đủ.
/// </summary>
public class AdminAuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;

    private const string TokenKey = "admin_jwt";
    private const string DefaultUsername = "admin";
    private const string DefaultPassword = "admin123";

    public AdminAuthService(HttpClient http, ILocalStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    /// <summary>
    /// Kiểm tra token hiện có trong localStorage còn valid không.
    /// Nếu không → login mới và lưu token.
    /// Gọi ở Program.cs khi app khởi động.
    /// </summary>
    public async Task<bool> EnsureLoggedInAsync()
    {
        try
        {
            var existing = await _storage.GetItemAsync<string>(TokenKey);
            if (!string.IsNullOrEmpty(existing))
            {
                // Verify bằng cách gọi /api/auth/me
                var req = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", existing);
                var res = await _http.SendAsync(req);
                if (res.IsSuccessStatusCode) return true;

                // Token hết hạn → xoá
                await _storage.RemoveItemAsync(TokenKey);
            }

            // Login mới
            var loginRes = await _http.PostAsJsonAsync("api/auth/login", new
            {
                username = DefaultUsername,
                password = DefaultPassword
            });

            if (!loginRes.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Admin auto-login failed: {loginRes.StatusCode}");
                return false;
            }

            var body = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
            if (body == null || string.IsNullOrEmpty(body.Token))
            {
                Console.WriteLine("❌ Admin login returned empty token");
                return false;
            }

            await _storage.SetItemAsync(TokenKey, body.Token);
            Console.WriteLine("✅ Admin auto-logged in");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Admin auto-login error: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try { return await _storage.GetItemAsync<string>(TokenKey); }
        catch { return null; }
    }

    private class LoginResponse
    {
        public string Token { get; set; } = "";
        public string Role { get; set; } = "";
    }
}

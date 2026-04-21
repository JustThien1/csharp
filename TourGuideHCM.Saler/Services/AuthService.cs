using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using TourGuideHCM.Saler.Models;

namespace TourGuideHCM.Saler.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private readonly AuthenticationStateProvider _authProvider;

    private const string TokenKey = "saler_jwt";
    private const string UserKey = "saler_user";

    public AuthService(
        HttpClient http,
        ILocalStorageService storage,
        AuthenticationStateProvider authProvider)
    {
        _http = http;
        _storage = storage;
        _authProvider = authProvider;
    }

    public async Task<(bool ok, string message)> LoginAsync(string username, string password)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/auth/login", new { username, password });
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, err?.Message ?? "Đăng nhập thất bại");
            }

            var body = await res.Content.ReadFromJsonAsync<AuthResponse>();
            if (body == null || string.IsNullOrEmpty(body.Token))
                return (false, "Response không hợp lệ");

            // App Saler chỉ dành cho role Saler
            if (body.Role != "Saler")
                return (false, "Tài khoản admin không thể đăng nhập từ app Saler. Vui lòng dùng trang Admin.");

            await _storage.SetItemAsync(TokenKey, body.Token);
            await _storage.SetItemAsync(UserKey, new UserInfo
            {
                UserId = body.UserId,
                Username = body.Username,
                FullName = body.FullName,
                Email = body.Email,
                Phone = body.Phone,
                Role = body.Role
            });

            ((CustomAuthStateProvider)_authProvider).NotifyUserAuthenticated();
            return (true, body.Message);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi kết nối: {ex.Message}");
        }
    }

    public async Task<(bool ok, string message)> RegisterAsync(RegisterRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/auth/register", req);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, err?.Message ?? "Đăng ký thất bại");
            }

            var body = await res.Content.ReadFromJsonAsync<AuthResponse>();
            if (body == null || string.IsNullOrEmpty(body.Token))
                return (false, "Response không hợp lệ");

            // Đăng ký xong tự login luôn
            await _storage.SetItemAsync(TokenKey, body.Token);
            await _storage.SetItemAsync(UserKey, new UserInfo
            {
                UserId = body.UserId,
                Username = body.Username,
                FullName = body.FullName,
                Email = body.Email,
                Phone = body.Phone,
                Role = body.Role
            });

            ((CustomAuthStateProvider)_authProvider).NotifyUserAuthenticated();
            return (true, body.Message);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi kết nối: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        await _storage.RemoveItemAsync(TokenKey);
        await _storage.RemoveItemAsync(UserKey);
        ((CustomAuthStateProvider)_authProvider).NotifyUserLoggedOut();
    }

    public async Task<string?> GetTokenAsync()
    {
        try { return await _storage.GetItemAsync<string>(TokenKey); }
        catch { return null; }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try { return await _storage.GetItemAsync<UserInfo>(UserKey); }
        catch { return null; }
    }

    public async Task<(bool ok, string message)> ChangePasswordAsync(string oldPassword, string newPassword)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/auth/change-password", new { oldPassword, newPassword });
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, err?.Message ?? "Đổi mật khẩu thất bại");
            }
            return (true, "Đổi mật khẩu thành công");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    private class ErrorResponse { public string? Message { get; set; } }
}

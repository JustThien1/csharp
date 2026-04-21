using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace TourGuideHCM.Saler.Services;

/// <summary>
/// Cung cấp authentication state cho Blazor từ JWT token trong localStorage.
/// Parse token để lấy claims (UserId, Role, etc.) mà không cần gọi API.
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _storage;
    private const string TokenKey = "saler_jwt";

    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthStateProvider(ILocalStorageService storage)
    {
        _storage = storage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _storage.GetItemAsync<string>(TokenKey);
            if (string.IsNullOrEmpty(token))
                return Anonymous;

            var claims = ParseClaimsFromJwt(token);
            if (claims == null)
                return Anonymous;

            // Check token expired
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (long.TryParse(expClaim, out var expSeconds))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                if (expDate <= DateTimeOffset.UtcNow)
                {
                    // Token hết hạn → xoá và trả anonymous
                    await _storage.RemoveItemAsync(TokenKey);
                    return Anonymous;
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return Anonymous;
        }
    }

    public void NotifyUserAuthenticated()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void NotifyUserLoggedOut()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static IEnumerable<Claim>? ParseClaimsFromJwt(string jwt)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            return token.Claims;
        }
        catch
        {
            return null;
        }
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services;

/// <summary>
/// Sinh JWT token cho user đã login. Token chứa Id, Username, Role để các controller
/// sau đó dùng [Authorize(Roles = "Admin")] hay lấy claim identity.
/// </summary>
public class JwtService
{
    private readonly IConfiguration _config;
    private const int ExpirationDays = 30;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user)
    {
        var key = _config["Jwt:Key"] ?? "your-super-secret-key-here-at-least-32-characters-long";
        var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Claims — thông tin nhúng trong token, server đọc lại sau
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("fullName", user.FullName ?? ""),
            new Claim("email", user.Email ?? "")
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(ExpirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

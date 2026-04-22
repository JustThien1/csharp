namespace TourGuideHCM.Saler.Models;

public class UserInfo
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "Saler";
    public DateTime? SubscriptionExpiresAt { get; set; }
    public bool HasActiveSubscription { get; set; }
}

/// <summary>Response từ API login/register.</summary>
public class AuthResponse
{
    public string Message { get; set; } = "";
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "";
    public DateTime? SubscriptionExpiresAt { get; set; }
    public bool HasActiveSubscription { get; set; }
}

public class ApiErrorResponse
{
    public string? Code { get; set; }
    public string? Message { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}

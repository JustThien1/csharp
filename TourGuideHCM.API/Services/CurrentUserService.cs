using System.Security.Claims;

namespace TourGuideHCM.API.Services;

/// <summary>
/// Helper để lấy thông tin user hiện tại từ JWT claims.
/// Inject vào các controller cần biết "ai đang gọi API này".
/// </summary>
public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }

    public string Role
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.Role)?.Value ?? "";
        }
    }

    public string Username
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.Name)?.Value ?? "";
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public bool IsAdmin => Role == "Admin";
    public bool IsSaler => Role == "Saler";
}

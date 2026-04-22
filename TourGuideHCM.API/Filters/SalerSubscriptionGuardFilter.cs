using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Filters;

/// <summary>
/// Chặn Saler khi gói (SubscriptionEndUtc) đã hết hạn hoặc chưa có ngày hết hạn hợp lệ.
/// Bỏ qua Admin, user chưa đăng nhập, và các API auth/danh mục để vẫn đăng nhập / xem thông tin tài khoản.
/// </summary>
public class SalerSubscriptionGuardFilter : IAsyncActionFilter
{
    private readonly CurrentUserService _currentUser;
    private readonly AppDbContext _db;

    public const string SubscriptionExpiredCode = "SUBSCRIPTION_EXPIRED";

    public SalerSubscriptionGuardFilter(CurrentUserService currentUser, AppDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsSaler || _currentUser.IsAdmin)
        {
            await next();
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();
        if (string.Equals(controller, "Auth", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(controller, "Category", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId);

        var end = user?.SubscriptionEndUtc;
        if (end == null || end <= DateTime.UtcNow)
        {
            context.Result = new ObjectResult(new
            {
                code = SubscriptionExpiredCode,
                message = "Gói sử dụng đã hết hạn. Vui lòng gia hạn để tiếp tục quản lý địa điểm và audio."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Filters;

public class RequireActiveSalerSubscriptionFilter : IAsyncActionFilter
{
    private readonly CurrentUserService _currentUser;
    private readonly AppDbContext _context;
    private readonly SubscriptionService _subscriptionService;

    public RequireActiveSalerSubscriptionFilter(
        CurrentUserService currentUser,
        AppDbContext context,
        SubscriptionService subscriptionService)
    {
        _currentUser = currentUser;
        _context = context;
        _subscriptionService = subscriptionService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.IsAdmin || !_currentUser.IsSaler)
        {
            await next();
            return;
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == _currentUser.UserId);

        if (_subscriptionService.HasActiveSubscription(user))
        {
            await next();
            return;
        }

        context.Result = new ObjectResult(new
        {
            code = "SubscriptionExpired",
            message = _subscriptionService.GetExpiredMessage(user?.SubscriptionExpiresAt),
            subscriptionExpiresAt = user?.SubscriptionExpiresAt
        })
        {
            StatusCode = StatusCodes.Status402PaymentRequired
        };
    }
}

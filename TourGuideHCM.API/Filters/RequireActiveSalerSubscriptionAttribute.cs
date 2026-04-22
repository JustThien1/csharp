using Microsoft.AspNetCore.Mvc;

namespace TourGuideHCM.API.Filters;

public class RequireActiveSalerSubscriptionAttribute : TypeFilterAttribute
{
    public RequireActiveSalerSubscriptionAttribute() : base(typeof(RequireActiveSalerSubscriptionFilter))
    {
    }
}

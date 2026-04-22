using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services;

public class SubscriptionService
{
    private readonly IConfiguration _configuration;

    public SubscriptionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int RenewalPriceVnd =>
        _configuration.GetValue<int?>("Payment:RenewalPriceVnd") ?? 299000;

    public int RenewalDurationDays =>
        _configuration.GetValue<int?>("Payment:RenewalDurationDays") ?? 30;

    public int StarterDurationDays =>
        _configuration.GetValue<int?>("Payment:StarterDurationDays") ?? RenewalDurationDays;

    public bool HasActiveSubscription(User? user)
    {
        if (user == null) return false;
        if (user.Role == "Admin") return true;
        return user.SubscriptionExpiresAt.HasValue && user.SubscriptionExpiresAt.Value >= DateTime.UtcNow;
    }

    public DateTime GetStarterExpiry(DateTime nowUtc)
    {
        return nowUtc.AddDays(StarterDurationDays);
    }

    public DateTime ExtendSubscription(User user, DateTime paidAtUtc)
    {
        var baseDate = user.SubscriptionExpiresAt.HasValue && user.SubscriptionExpiresAt > paidAtUtc
            ? user.SubscriptionExpiresAt.Value
            : paidAtUtc;

        var nextExpiry = baseDate.AddDays(RenewalDurationDays);
        user.SubscriptionExpiresAt = nextExpiry;
        return nextExpiry;
    }

    public string GetExpiredMessage(DateTime? expiresAtUtc)
    {
        if (expiresAtUtc.HasValue)
        {
            return $"Gói Saler đã hết hạn từ {expiresAtUtc.Value.ToLocalTime():dd/MM/yyyy HH:mm}. Vui lòng gia hạn để tiếp tục.";
        }

        return "Tài khoản Saler chưa có gói sử dụng. Vui lòng gia hạn để tiếp tục.";
    }
}

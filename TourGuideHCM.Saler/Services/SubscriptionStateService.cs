namespace TourGuideHCM.Saler.Services;

public class SubscriptionStateService
{
    public string? Message { get; private set; }
    public DateTime? SubscriptionExpiresAt { get; private set; }

    public event Action? Changed;

    public void ShowExpired(string? message, DateTime? subscriptionExpiresAt)
    {
        Message = string.IsNullOrWhiteSpace(message)
            ? "Gói Saler đã hết hạn. Vui lòng gia hạn để tiếp tục."
            : message;
        SubscriptionExpiresAt = subscriptionExpiresAt;
        Changed?.Invoke();
    }

    public void Clear()
    {
        if (Message == null && SubscriptionExpiresAt == null) return;
        Message = null;
        SubscriptionExpiresAt = null;
        Changed?.Invoke();
    }
}

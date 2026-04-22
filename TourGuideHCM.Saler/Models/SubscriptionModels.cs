namespace TourGuideHCM.Saler.Models;

public class SubscriptionInfoResponse
{
    public int RenewalPriceVnd { get; set; }
    public int RenewalDurationDays { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public bool HasActiveSubscription { get; set; }
    public bool CanSimulatePayment { get; set; }
    public PaymentInfoDto PaymentInfo { get; set; } = new();
}

public class RenewalRequestResponse
{
    public string Message { get; set; } = "";
    public int PaymentId { get; set; }
    public string PaymentStatus { get; set; } = "";
    public string ProviderReference { get; set; } = "";
    public int AmountVnd { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public PaymentInfoDto PaymentInfo { get; set; } = new();
}

public class PaymentInfoDto
{
    public string BankCode { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string TransferContent { get; set; } = "";
    public int AmountVnd { get; set; }
    public string QrImageUrl { get; set; } = "";
    public string Note { get; set; } = "";
}

public class SimulatePaymentResponse
{
    public string Message { get; set; } = "";
    public int PaymentId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
}

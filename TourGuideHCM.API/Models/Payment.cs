namespace TourGuideHCM.API.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AmountVnd { get; set; }
        public string Status { get; set; } = "Pending";
        public string Provider { get; set; } = "VietQR";
        public string ProviderReference { get; set; } = string.Empty;
        public string TransferContent { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public DateTime? SubscriptionExpiresAtBefore { get; set; }
        public DateTime? SubscriptionExpiresAtAfter { get; set; }

        public User? User { get; set; }
    }
}

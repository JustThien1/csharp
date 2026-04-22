namespace TourGuideHCM.Admin.Models;

public class PaymentHistoryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string? FullName { get; set; }
    public int AmountVnd { get; set; }
    public string Status { get; set; } = "";
    public string Provider { get; set; } = "";
    public string ProviderReference { get; set; } = "";
    public string TransferContent { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? SubscriptionExpiresAtBefore { get; set; }
    public DateTime? SubscriptionExpiresAtAfter { get; set; }
    public string? Note { get; set; }
}

public class PaymentHistoryPageDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<PaymentHistoryDto> Items { get; set; } = new();
}

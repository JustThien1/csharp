using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CurrentUserService _currentUser;
    private readonly SubscriptionService _subscriptionService;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public PaymentController(
        AppDbContext context,
        CurrentUserService currentUser,
        SubscriptionService subscriptionService,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _subscriptionService = subscriptionService;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet("subscription-info")]
    [Authorize]
    public async Task<IActionResult> GetSubscriptionInfo()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

        if (user.Role != "Saler")
            return BadRequest(new { message = "Chỉ tài khoản Saler mới dùng tính năng này." });

        return Ok(new
        {
            renewalPriceVnd = _subscriptionService.RenewalPriceVnd,
            renewalDurationDays = _subscriptionService.RenewalDurationDays,
            subscriptionExpiresAt = user.SubscriptionExpiresAt,
            hasActiveSubscription = _subscriptionService.HasActiveSubscription(user),
            canSimulatePayment = _environment.IsDevelopment(),
            paymentInfo = BuildPaymentInfo(user)
        });
    }

    [HttpPost("renewal-request")]
    [Authorize]
    public async Task<IActionResult> CreateRenewalRequest()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

        if (user.Role != "Saler")
            return BadRequest(new { message = "Chỉ tài khoản Saler mới có thể gia hạn." });

        var payment = new Payment
        {
            UserId = user.Id,
            AmountVnd = _subscriptionService.RenewalPriceVnd,
            Status = "Pending",
            Provider = "VietQR",
            ProviderReference = $"PAY-{Guid.NewGuid():N}".ToUpperInvariant(),
            TransferContent = $"SALER{user.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            SubscriptionExpiresAtBefore = user.SubscriptionExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Đã tạo yêu cầu thanh toán. Vui lòng quét mã QR để gia hạn.",
            paymentId = payment.Id,
            paymentStatus = payment.Status,
            providerReference = payment.ProviderReference,
            amountVnd = payment.AmountVnd,
            subscriptionExpiresAt = user.SubscriptionExpiresAt,
            paymentInfo = BuildPaymentInfo(user, payment.TransferContent)
        });
    }

    [HttpPost("dev/simulate-success/{paymentId:int}")]
    [Authorize]
    public async Task<IActionResult> SimulateSuccess(int paymentId)
    {
        if (!_environment.IsDevelopment())
            return NotFound(new { message = "Endpoint này chỉ bật ở môi trường phát triển." });

        var payment = await _context.Payments
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        if (payment == null)
            return NotFound(new { message = "Không tìm thấy giao dịch." });

        if (!_currentUser.IsAdmin && payment.UserId != _currentUser.UserId)
            return Forbid();

        if (payment.User == null)
            return BadRequest(new { message = "Không tìm thấy tài khoản nhận gia hạn." });

        if (payment.Status == "Paid")
        {
            return Ok(new
            {
                message = "Giao dịch đã được xác nhận trước đó.",
                paymentId = payment.Id,
                subscriptionExpiresAt = payment.User.SubscriptionExpiresAt
            });
        }

        var paidAt = DateTime.UtcNow;
        payment.Status = "Paid";
        payment.PaidAt = paidAt;
        payment.SubscriptionExpiresAtBefore = payment.User.SubscriptionExpiresAt;
        payment.SubscriptionExpiresAtAfter = _subscriptionService.ExtendSubscription(payment.User, paidAt);
        payment.Note = "Thanh toán mô phỏng thành công trong môi trường Development.";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Gia hạn thành công thêm {_subscriptionService.RenewalDurationDays} ngày.",
            paymentId = payment.Id,
            paidAt = payment.PaidAt,
            subscriptionExpiresAt = payment.User.SubscriptionExpiresAt
        });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public IActionResult Webhook()
    {
        if (_environment.IsDevelopment())
        {
            return Ok(new { message = "Webhook sandbox chưa được tích hợp. Hãy dùng endpoint simulate ở môi trường Development." });
        }

        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Webhook thanh toán thực tế chưa được cấu hình. TODO: xác thực chữ ký theo cổng thanh toán."
        });
    }

    [HttpGet("admin/history")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? userId = null,
        [FromQuery] string? username = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        var query = _context.Payments
            .AsNoTracking()
            .Include(x => x.User)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(username))
        {
            var keyword = username.Trim().ToLower();
            query = query.Where(x =>
                x.User != null &&
                (x.User.Username.ToLower().Contains(keyword) ||
                 (x.User.FullName != null && x.User.FullName.ToLower().Contains(keyword))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (fromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtcExclusive = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(x => x.CreatedAt < toUtcExclusive);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                id = x.Id,
                userId = x.UserId,
                username = x.User != null ? x.User.Username : string.Empty,
                fullName = x.User != null ? x.User.FullName : null,
                amountVnd = x.AmountVnd,
                status = x.Status,
                provider = x.Provider,
                providerReference = x.ProviderReference,
                transferContent = x.TransferContent,
                createdAt = x.CreatedAt,
                paidAt = x.PaidAt,
                subscriptionExpiresAtBefore = x.SubscriptionExpiresAtBefore,
                subscriptionExpiresAtAfter = x.SubscriptionExpiresAtAfter,
                note = x.Note
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            items
        });
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        if (_currentUser.UserId <= 0) return null;
        return await _context.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId);
    }

    private object BuildPaymentInfo(User user, string? transferContent = null)
    {
        var bankCode = _configuration["Payment:BankCode"] ?? "970422";
        var accountNumber = _configuration["Payment:AccountNumber"] ?? "0000000000";
        var accountName = _configuration["Payment:AccountName"] ?? "TOURGUIDE HCM";
        var finalTransferContent = transferContent ?? $"SALER{user.Id}";
        var amount = _subscriptionService.RenewalPriceVnd;
        var qrImageUrl = $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.png?amount={amount}&addInfo={Uri.EscapeDataString(finalTransferContent)}&accountName={Uri.EscapeDataString(accountName)}";

        return new
        {
            bankCode,
            accountNumber,
            accountName,
            transferContent = finalTransferContent,
            amountVnd = amount,
            qrImageUrl,
            note = $"Gia hạn: {amount:N0}đ / lần"
        };
    }
}

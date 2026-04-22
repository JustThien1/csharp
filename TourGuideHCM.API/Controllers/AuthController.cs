using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;

        public AuthController(AppDbContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        // ================= REGISTER (chỉ cho Saler) =================
        // Saler đăng ký trực tiếp từ app Saler.
        // Admin được tạo seed từ Program.cs khi DB khởi động lần đầu.
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Vui lòng nhập đầy đủ username và password" });

            if (req.Password.Length < 6)
                return BadRequest(new { message = "Mật khẩu tối thiểu 6 ký tự" });

            if (await _context.Users.AnyAsync(x => x.Username == req.Username))
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại" });

            if (!string.IsNullOrEmpty(req.Email) &&
                await _context.Users.AnyAsync(x => x.Email == req.Email))
                return BadRequest(new { message = "Email đã được sử dụng" });

            var user = new User
            {
                Username = req.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                FullName = req.FullName,
                Email = req.Email,
                Phone = req.Phone,
                Role = "Saler",                  // Đăng ký công khai = Saler
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SubscriptionEndUtc = DateTime.UtcNow.AddDays(14)   // dùng thử, sau đó cần gia hạn
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                message = "Đăng ký thành công",
                token,
                userId = user.Id,
                username = user.Username,
                fullName = user.FullName,
                role = user.Role,
                subscriptionEndUtc = user.SubscriptionEndUtc
            });
        }

        // ================= LOGIN =================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Thiếu thông tin đăng nhập" });

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == req.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Tài khoản đã bị khoá. Liên hệ admin." });

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                message = "Đăng nhập thành công",
                token,
                userId = user.Id,
                username = user.Username,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                subscriptionEndUtc = user.SubscriptionEndUtc
            });
        }

        // ================= ME (verify token, trả thông tin current user) =================
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                userId = user.Id,
                username = user.Username,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                isActive = user.IsActive,
                subscriptionEndUtc = user.SubscriptionEndUtc
            });
        }

        // ================= CHANGE PASSWORD =================
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
                return BadRequest(new { message = "Mật khẩu mới tối thiểu 6 ký tự" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }

        // ====================== DTOs ======================
        public class RegisterRequest
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";    // plain-text, sẽ được hash
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
        }

        public class LoginRequest
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }

        public class ChangePasswordRequest
        {
            public string OldPassword { get; set; } = "";
            public string NewPassword { get; set; } = "";
        }
    }
}

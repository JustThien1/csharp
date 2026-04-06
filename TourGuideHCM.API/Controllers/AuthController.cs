using Microsoft.AspNetCore.Mvc;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // ================= REGISTER =================
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.PasswordHash))
                return BadRequest("Username và Password không được để trống");

            if (_context.Users.Any(x => x.Username == request.Username))
                return BadRequest("Username đã tồn tại");

            var user = new User
            {
                Username = request.Username,
                PasswordHash = request.PasswordHash,
                FullName = request.FullName,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Đăng ký thành công",
                userId = user.Id
            });
        }

        // ================= LOGIN =================
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.PasswordHash))
                return BadRequest("Thiếu thông tin đăng nhập");

            var user = _context.Users
                .FirstOrDefault(x => x.Username == request.Username
                                  && x.PasswordHash == request.PasswordHash);

            if (user == null)
                return Unauthorized("Sai tài khoản hoặc mật khẩu");

            return Ok(new
            {
                message = "Đăng nhập thành công",
                userId = user.Id,
                username = user.Username
            });
        }
    }
}
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

        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.PasswordHash))
                return BadRequest("Username và Password không được để trống");

            // Kiểm tra username đã tồn tại chưa
            if (_context.Users.Any(x => x.Username == user.Username))
                return BadRequest("Username đã tồn tại");

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "Đăng ký thành công", userId = user.Id });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User loginUser)
        {
            var user = _context.Users
                .FirstOrDefault(x => x.Username == loginUser.Username
                                  && x.PasswordHash == loginUser.PasswordHash);

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
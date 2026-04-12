using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetAll()
        {
            // Cách tối ưu: Group PlaybackLogs trước rồi join với Users
            var playbackCounts = await _context.PlaybackLogs
                .GroupBy(pl => pl.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalListens = g.Count()
                })
                .ToDictionaryAsync(x => x.UserId, x => x.TotalListens);

            var users = await _context.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName ?? u.Username ?? "Chưa có tên",
                    Email = u.Email ?? "",
                    Phone = "",
                    Role = "User",
                    IsActive = true,
                    CreatedDate = u.CreatedAt,
                    TotalListens = playbackCounts.GetValueOrDefault(u.Id, 0)   // Lấy từ dictionary
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/{id}  (dùng cho Edit nếu cần)
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            var totalListens = await _context.PlaybackLogs
                .CountAsync(pl => pl.UserId == id);

            var dto = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName ?? user.Username ?? "Chưa có tên",
                Email = user.Email ?? "",
                Phone = "",
                Role = "User",
                IsActive = true,
                CreatedDate = user.CreatedAt,
                TotalListens = totalListens
            };

            return Ok(dto);
        }

        // Các phương thức Create, Update, Delete giữ nguyên như cũ
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName) && string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Vui lòng nhập họ tên hoặc email");

            if (!string.IsNullOrEmpty(dto.Email) &&
                await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email này đã được sử dụng");

            var user = new User
            {
                Username = string.IsNullOrEmpty(dto.Email)
                    ? $"user_{DateTime.UtcNow.Ticks}"
                    : dto.Email.Split('@')[0],
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = "temp_hash_123",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            dto.Id = user.Id;
            dto.CreatedDate = user.CreatedAt;
            dto.TotalListens = 0;

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            user.FullName = dto.FullName;
            user.Email = dto.Email;

            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa người dùng thành công" });
        }
    }
}
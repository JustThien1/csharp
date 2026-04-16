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
            // Đếm từ PlaybackHistories (app ghi vào đây)
            var countFromHistory = await _context.PlaybackHistories
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            // Đếm từ PlaybackLogs (cũ) — lọc null UserId trước
            var countFromLogs = await _context.PlaybackLogs
                .Where(p => p.UserId != null)
                .GroupBy(p => p.UserId!)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName ?? u.Username ?? "Chưa có tên",
                    Email = u.Email ?? "",
                    Phone = u.Phone ?? "",
                    Role = "User",
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedAt,
                    TotalListens = 0 // tính sau
                })
                .ToListAsync();

            // Gán TotalListens sau khi load
            foreach (var u in users)
            {
                var fromHistory = countFromHistory.GetValueOrDefault(u.Id, 0);
                var fromLogs = countFromLogs.GetValueOrDefault(u.Id, 0);
                u.TotalListens = fromHistory + fromLogs;
            }

            return Ok(users);
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            var fromHistory = await _context.PlaybackHistories.CountAsync(p => p.UserId == id);
            var fromLogs = await _context.PlaybackLogs.CountAsync(p => p.UserId == id);

            return Ok(new UserDto
            {
                Id = user.Id,
                FullName = user.FullName ?? user.Username ?? "Chưa có tên",
                Email = user.Email ?? "",
                Phone = user.Phone ?? "",
                Role = "User",
                IsActive = user.IsActive,
                CreatedDate = user.CreatedAt,
                TotalListens = fromHistory + fromLogs
            });
        }

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
            user.Phone = dto.Phone;   // ← Lưu SĐT

            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        [HttpPut("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id, [FromBody] ToggleActiveDto? dto = null)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            user.IsActive = dto != null ? dto.IsActive : !user.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { isActive = user.IsActive, message = "Đã cập nhật trạng thái" });
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
    public class ToggleActiveDto { public bool IsActive { get; set; } }

}
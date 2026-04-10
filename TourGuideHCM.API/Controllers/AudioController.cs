using Microsoft.AspNetCore.Mvc;
using TourGuideHCM.API.Data;           // AppDbContext của bạn
using TourGuideHCM.API.Models;
using System.IO;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AudioController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ====================== UPLOAD AUDIO ======================
        [HttpPost("upload")]
        public async Task<IActionResult> UploadAudio(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file nào được chọn.");

            // Chỉ cho phép file audio
            var allowedTypes = new[] { "audio/mpeg", "audio/wav", "audio/x-m4a", "audio/ogg" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Chỉ chấp nhận file audio (.mp3, .wav, .m4a, .ogg)");

            try
            {
                // Tạo thư mục lưu file (wwwroot/audio)
                var uploadPath = Path.Combine(_env.WebRootPath, "audio");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                // Tạo tên file unique
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var fullPath = Path.Combine(uploadPath, fileName);

                // Lưu file
                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                // URL trả về (client sẽ dùng đường dẫn này)
                var audioUrl = $"/audio/{fileName}";

                return Ok(audioUrl);   // Trả về string URL
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi upload: {ex.Message}");
            }
        }

        // ====================== CRUD Audio ======================
        [HttpGet]
        public IActionResult GetAll()
        {
            var audios = _context.Audios.ToList();   // Giả sử bạn có DbSet<Audio>
            return Ok(audios);
        }

        [HttpGet("poi/{poiId}")]
        public IActionResult GetByPoiId(int poiId)
        {
            var audios = _context.Audios.Where(a => a.PoiId == poiId).ToList();
            return Ok(audios);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Audio audio)
        {
            if (audio == null || audio.PoiId == 0)
                return BadRequest("Dữ liệu không hợp lệ");

            _context.Audios.Add(audio);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetAll), new { id = audio.Id }, audio);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Audio updated)
        {
            var audio = _context.Audios.Find(id);
            if (audio == null) return NotFound();

            audio.Language = updated.Language;
            audio.AudioUrl = updated.AudioUrl;
            audio.DurationSeconds = updated.DurationSeconds;
            audio.Description = updated.Description;
            audio.IsActive = updated.IsActive;

            _context.SaveChanges();
            return Ok(audio);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var audio = _context.Audios.Find(id);
            if (audio == null) return NotFound();

            _context.Audios.Remove(audio);
            _context.SaveChanges();
            return Ok(new { message = "Xóa thành công" });
        }
    }
}
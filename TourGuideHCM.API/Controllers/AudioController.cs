using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public AudioController(AppDbContext context, IWebHostEnvironment env, IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config;
        }

        // ── CONVERT TEXT → AUDIO (TTS + lưu DB luôn) ──────────────────────────
        [HttpPost("convert")]
        public async Task<IActionResult> ConvertTts([FromBody] TtsConvertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text))
                return BadRequest("Vui lòng nhập nội dung text");
            if (dto.PoiId == 0)
                return BadRequest("Vui lòng chọn POI");

            // Lấy API key từ config
            var apiKey = _config["GoogleTTS:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(500, "Chưa cấu hình Google TTS API Key trong appsettings.json");

            // Map language + gender → Google voice
            var (languageCode, voiceName) = (dto.Language, dto.Gender) switch
            {
                ("vi", "female") => ("vi-VN", "vi-VN-Wavenet-A"),
                ("vi", "male") => ("vi-VN", "vi-VN-Wavenet-B"),
                ("en", "female") => ("en-US", "en-US-Wavenet-F"),
                ("en", "male") => ("en-US", "en-US-Wavenet-D"),
                ("zh", "female") => ("cmn-CN", "cmn-CN-Wavenet-A"),
                ("zh", "male") => ("cmn-CN", "cmn-CN-Wavenet-B"),
                ("ko", "female") => ("ko-KR", "ko-KR-Wavenet-A"),
                ("ko", "male") => ("ko-KR", "ko-KR-Wavenet-C"),
                _ => ("vi-VN", "vi-VN-Wavenet-A")
            };

            try
            {
                // Gọi Google Cloud TTS
                var requestBody = new
                {
                    input = new { text = dto.Text },
                    voice = new { languageCode, name = voiceName },
                    audioConfig = new { audioEncoding = "MP3", speakingRate = dto.Speed }
                };

                using var httpClient = new HttpClient();
                var url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";
                var response = await httpClient.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"Google TTS lỗi: {err}");
                }

                var ttsResult = await response.Content.ReadFromJsonAsync<GoogleTtsResponse>();
                if (ttsResult?.AudioContent == null)
                    return StatusCode(500, "Google TTS không trả về audio");

                // Lưu file MP3
                var audioBytes = Convert.FromBase64String(ttsResult.AudioContent);
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var uploadPath = Path.Combine(webRoot, "audio");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var fileName = $"tts_{dto.Language}_{Guid.NewGuid()}.mp3";
                var fullPath = Path.Combine(uploadPath, fileName);
                await System.IO.File.WriteAllBytesAsync(fullPath, audioBytes);

                // Lưu relative URL — App tự resolve qua BaseUrl
                var audioUrl = $"/audio/{fileName}";

                // Ước tính thời lượng
                var wordCount = dto.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                var duration = (int)Math.Ceiling(wordCount / (dto.Speed * 2.5));

                // Xóa audio cũ cùng POI + language nếu có
                var existing = await _context.Audios
                    .Where(a => a.PoiId == dto.PoiId && a.Language == dto.Language)
                    .ToListAsync();

                foreach (var old in existing)
                {
                    // Xóa file vật lý
                    try
                    {
                        var oldFile = Path.Combine(webRoot, "audio", Path.GetFileName(old.AudioUrl ?? ""));
                        if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
                    }
                    catch { }
                }
                _context.Audios.RemoveRange(existing);

                // Tạo Audio record mới
                var audio = new Audio
                {
                    PoiId = dto.PoiId,
                    Language = dto.Language,
                    AudioUrl = audioUrl,
                    DurationSeconds = duration,
                    Description = dto.Text.Length > 100 ? dto.Text[..100] + "…" : dto.Text,
                    IsActive = true
                };
                _context.Audios.Add(audio);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = audio.Id,
                    audioUrl,
                    duration,
                    message = $"✅ Tạo audio thành công ({duration}s)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        // ── UPLOAD FILE ────────────────────────────────────────────────────────
        [HttpPost("upload")]
        public async Task<IActionResult> UploadAudio(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file nào được chọn.");

            var allowedTypes = new[] { "audio/mpeg", "audio/mp3", "audio/wav",
                                       "audio/x-m4a", "audio/ogg", "audio/aac" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest("Chỉ chấp nhận file audio (.mp3, .wav, .m4a, .ogg, .aac)");

            if (file.Length > 20 * 1024 * 1024)
                return BadRequest("File quá lớn! Tối đa 20MB.");

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadPath = Path.Combine(webRoot, "audio");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var ext = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadPath, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Lưu relative URL — App tự resolve qua BaseUrl
            var audioUrl = $"/audio/{fileName}";
            return Ok(audioUrl);
        }

        // ── GET ALL ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var audios = await _context.Audios
                .Include(a => a.POI)
                .OrderByDescending(a => a.Id)
                .Select(a => new AudioDto
                {
                    Id = a.Id,
                    PoiId = a.PoiId,
                    PoiName = a.POI != null ? a.POI.Name : $"POI_{a.PoiId}",
                    Language = a.Language ?? "vi",
                    AudioUrl = a.AudioUrl ?? "",
                    DurationSeconds = a.DurationSeconds,
                    Description = a.Description ?? "",
                    IsActive = a.IsActive
                })
                .ToListAsync();
            return Ok(audios);
        }

        // ── GET BY POI ─────────────────────────────────────────────────────────
        [HttpGet("poi/{poiId}")]
        public async Task<IActionResult> GetByPoiId(int poiId)
        {
            var audios = await _context.Audios
                .Include(a => a.POI)
                .Where(a => a.PoiId == poiId)
                .Select(a => new AudioDto
                {
                    Id = a.Id,
                    PoiId = a.PoiId,
                    PoiName = a.POI != null ? a.POI.Name : $"POI_{a.PoiId}",
                    Language = a.Language ?? "vi",
                    AudioUrl = a.AudioUrl ?? "",
                    DurationSeconds = a.DurationSeconds,
                    Description = a.Description ?? "",
                    IsActive = a.IsActive
                })
                .ToListAsync();
            return Ok(audios);
        }

        // ── CREATE ─────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AudioDto dto)
        {
            if (dto.PoiId == 0) return BadRequest("Vui lòng chọn POI");
            var audio = new Audio
            {
                PoiId = dto.PoiId,
                Language = dto.Language ?? "vi",
                AudioUrl = dto.AudioUrl ?? "",
                DurationSeconds = dto.DurationSeconds,
                Description = dto.Description,
                IsActive = dto.IsActive
            };
            _context.Audios.Add(audio);
            await _context.SaveChangesAsync();
            dto.Id = audio.Id;
            return Ok(dto);
        }

        // ── UPDATE ─────────────────────────────────────────────────────────────
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AudioDto dto)
        {
            var audio = await _context.Audios.FindAsync(id);
            if (audio == null) return NotFound("Không tìm thấy audio");
            audio.PoiId = dto.PoiId;
            audio.Language = dto.Language ?? audio.Language;
            audio.DurationSeconds = dto.DurationSeconds;
            audio.Description = dto.Description;
            audio.IsActive = dto.IsActive;
            if (!string.IsNullOrEmpty(dto.AudioUrl)) audio.AudioUrl = dto.AudioUrl;
            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        // ── DELETE ─────────────────────────────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var audio = await _context.Audios.FindAsync(id);
            if (audio == null) return NotFound();
            try
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var filePath = Path.Combine(webRoot, "audio", Path.GetFileName(audio.AudioUrl ?? ""));
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }
            catch { }
            _context.Audios.Remove(audio);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Xóa thành công" });
        }
    }

    public class AudioDto { public int Id { get; set; } public int PoiId { get; set; } public string PoiName { get; set; } = ""; public string Language { get; set; } = "vi"; public string AudioUrl { get; set; } = ""; public int DurationSeconds { get; set; } public string Description { get; set; } = ""; public bool IsActive { get; set; } = true; }
    public class TtsConvertDto { public int PoiId { get; set; } public string Text { get; set; } = ""; public string Language { get; set; } = "vi"; public string Gender { get; set; } = "female"; public double Speed { get; set; } = 1.0; }
    public class GoogleTtsResponse { public string? AudioContent { get; set; } }
}
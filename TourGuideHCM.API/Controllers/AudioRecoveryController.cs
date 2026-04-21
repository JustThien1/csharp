using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Controllers;

/// <summary>
/// Khôi phục audio files khi DB bị xoá nhưng file MP3 vẫn còn trong wwwroot/audio.
/// Quét folder, tìm file không có record trong bảng Audios, cho admin gán lại vào POI.
/// </summary>
[ApiController]
[Route("api/audio-recovery")]
public class AudioRecoveryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public AudioRecoveryController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    /// <summary>
    /// Liệt kê các file audio trong wwwroot/audio chưa có record trong bảng Audios.
    /// </summary>
    [HttpGet("orphans")]
    public async Task<IActionResult> GetOrphans()
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var audioPath = Path.Combine(webRoot, "audio");

        if (!Directory.Exists(audioPath))
            return Ok(new List<object>());

        // Tất cả file mp3/wav/m4a trong folder
        var allFiles = Directory.GetFiles(audioPath)
            .Where(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext == ".mp3" || ext == ".wav" || ext == ".m4a" || ext == ".ogg";
            })
            .Select(f => Path.GetFileName(f))
            .ToList();

        // Các URL đã có trong DB
        var existingUrls = await _context.Audios
            .Select(a => a.AudioUrl)
            .ToListAsync();

        var existingFileNames = existingUrls
            .Where(u => !string.IsNullOrEmpty(u))
            .Select(u => Path.GetFileName(u!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Cũng check cột AudioUrl cũ trong POI
        var poiAudioUrls = await _context.POIs
            .Where(p => p.AudioUrl != null && p.AudioUrl != "")
            .Select(p => p.AudioUrl!)
            .ToListAsync();

        foreach (var u in poiAudioUrls)
            existingFileNames.Add(Path.GetFileName(u));

        // File orphan = có trong folder, không có trong DB
        var orphans = allFiles
            .Where(f => !existingFileNames.Contains(f))
            .Select(f =>
            {
                var info = new FileInfo(Path.Combine(audioPath, f));
                return new OrphanFileDto
                {
                    FileName = f,
                    Url = $"/audio/{f}",
                    SizeKB = (int)Math.Round(info.Length / 1024.0),
                    ModifiedAt = info.LastWriteTime,
                    // Đoán ngôn ngữ từ prefix tên file: "tts_vi_xxx.mp3" → "vi"
                    GuessedLanguage = GuessLanguage(f)
                };
            })
            .OrderBy(x => x.FileName)
            .ToList();

        return Ok(orphans);
    }

    /// <summary>
    /// Gán 1 file audio orphan vào POI cụ thể.
    /// </summary>
    [HttpPost("restore")]
    public async Task<IActionResult> Restore([FromBody] RestoreRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FileName))
            return BadRequest(new { message = "Thiếu FileName" });
        if (req.PoiId <= 0)
            return BadRequest(new { message = "Thiếu PoiId" });

        // Verify file tồn tại
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, "audio", req.FileName);
        if (!System.IO.File.Exists(fullPath))
            return NotFound(new { message = $"File {req.FileName} không tồn tại trong folder audio" });

        // Verify POI tồn tại
        var poi = await _context.POIs.FindAsync(req.PoiId);
        if (poi == null)
            return NotFound(new { message = $"Không tìm thấy POI #{req.PoiId}" });

        // Tạo record mới
        var audio = new Audio
        {
            PoiId = req.PoiId,
            Language = string.IsNullOrWhiteSpace(req.Language) ? "vi" : req.Language,
            AudioUrl = $"/audio/{req.FileName}",
            DurationSeconds = req.DurationSeconds ?? 0,
            Description = req.Description ?? $"Khôi phục từ file {req.FileName}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Audios.Add(audio);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = audio.Id,
            audioUrl = audio.AudioUrl,
            poiName = poi.Name,
            message = $"✅ Đã gán {req.FileName} → {poi.Name} ({audio.Language})"
        });
    }

    /// <summary>
    /// Xoá 1 file orphan (không liên kết đến DB, an toàn).
    /// Dùng khi admin biết file đó không cần thiết.
    /// </summary>
    [HttpDelete("orphans/{fileName}")]
    public IActionResult DeleteOrphan(string fileName)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, "audio", fileName);

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        try
        {
            System.IO.File.Delete(fullPath);
            return Ok(new { message = $"Đã xoá file {fileName}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ====================== HELPERS ======================
    private static string GuessLanguage(string fileName)
    {
        var lower = fileName.ToLowerInvariant();
        if (lower.StartsWith("tts_vi")) return "vi";
        if (lower.StartsWith("tts_en")) return "en";
        if (lower.StartsWith("tts_zh")) return "zh";
        if (lower.StartsWith("tts_ko")) return "ko";
        return "vi";   // mặc định
    }

    // ====================== DTOs ======================
    public class OrphanFileDto
    {
        public string FileName { get; set; } = "";
        public string Url { get; set; } = "";
        public int SizeKB { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string GuessedLanguage { get; set; } = "vi";
    }

    public class RestoreRequest
    {
        public string FileName { get; set; } = "";
        public int PoiId { get; set; }
        public string? Language { get; set; }
        public int? DurationSeconds { get; set; }
        public string? Description { get; set; }
    }
}

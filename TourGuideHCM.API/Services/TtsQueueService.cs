using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services;

/// <summary>
/// Thao tác với hàng đợi TTS. Không xử lý trực tiếp — worker nền TtsWorker sẽ tự pick job.
/// </summary>
public class TtsQueueService
{
    private readonly AppDbContext _context;

    public TtsQueueService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Thêm job TTS vào hàng đợi. Worker sẽ tự xử lý trong 3s.
    /// </summary>
    public async Task<TtsJob> EnqueueAsync(int poiId, string text, string language = "vi",
                                           string gender = "female", double speed = 1.0)
    {
        var job = new TtsJob
        {
            PoiId = poiId,
            Text = text,
            Language = language,
            Gender = gender,
            Speed = speed,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.TtsJobs.Add(job);
        await _context.SaveChangesAsync();
        return job;
    }

    public async Task<List<TtsJob>> GetQueueAsync(int limit = 100)
    {
        return await _context.TtsJobs
            .Include(j => j.POI)
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<TtsQueueStats> GetStatsAsync()
    {
        var all = await _context.TtsJobs
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new TtsQueueStats
        {
            Pending = all.FirstOrDefault(x => x.Status == "Pending")?.Count ?? 0,
            Processing = all.FirstOrDefault(x => x.Status == "Processing")?.Count ?? 0,
            Completed = all.FirstOrDefault(x => x.Status == "Completed")?.Count ?? 0,
            Failed = all.FirstOrDefault(x => x.Status == "Failed")?.Count ?? 0
        };
    }

    public async Task<bool> RetryAsync(int jobId)
    {
        var job = await _context.TtsJobs.FindAsync(jobId);
        if (job == null || job.Status == "Processing") return false;

        job.Status = "Pending";
        job.ErrorMessage = null;
        job.RetryCount = 0;   // Reset retry count khi admin manual retry
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelAsync(int jobId)
    {
        var job = await _context.TtsJobs.FindAsync(jobId);
        if (job == null) return false;

        // Không huỷ được job đang processing
        if (job.Status == "Processing") return false;

        _context.TtsJobs.Remove(job);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> ClearFailedAsync()
    {
        var failedJobs = await _context.TtsJobs
            .Where(j => j.Status == "Failed")
            .ToListAsync();

        _context.TtsJobs.RemoveRange(failedJobs);
        await _context.SaveChangesAsync();
        return failedJobs.Count;
    }

    /// <summary>
    /// Xoá các job Completed cũ hơn N giờ để DB không phình. Worker tự gọi định kỳ.
    /// </summary>
    public async Task<int> CleanupOldCompletedAsync(int hoursOld = 24)
    {
        var threshold = DateTime.UtcNow.AddHours(-hoursOld);
        var oldJobs = await _context.TtsJobs
            .Where(j => j.Status == "Completed" && j.CompletedAt < threshold)
            .ToListAsync();

        _context.TtsJobs.RemoveRange(oldJobs);
        await _context.SaveChangesAsync();
        return oldJobs.Count;
    }
}

public class TtsQueueStats
{
    public int Pending { get; set; }
    public int Processing { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
}

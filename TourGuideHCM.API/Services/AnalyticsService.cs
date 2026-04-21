using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Services;

public class AnalyticsService
{
    private readonly AppDbContext _context;

    public AnalyticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;

        try
        {
            // 1. Số người online (có hoạt động trong 10 phút gần nhất)
            var onlineUsers = await _context.PlaybackLogs
                .Where(l => l.TriggeredAt >= now.AddMinutes(-10))
                .Select(l => l.UserId)
                .Distinct()
                .CountAsync();

            // 2. Số người đang nghe audio (có log trong 2 phút gần nhất)
            var listeningUsers = await _context.PlaybackLogs
                .Where(l => l.TriggeredAt >= now.AddMinutes(-2))
                .Select(l => l.UserId)
                .Distinct()
                .CountAsync();

            // 3. POI nóng nhất trong 24h
            var hotPoiData = await _context.PlaybackLogs
                .Where(l => l.TriggeredAt >= now.AddHours(-24))
                .Include(l => l.POI)
                .GroupBy(l => l.POIId)
                .Select(g => new
                {
                    POIId = g.Key,
                    Count = g.Count(),
                    PoiName = g.FirstOrDefault()!.POI != null
                              ? g.FirstOrDefault()!.POI.Name
                              : $"POI #{g.Key}"
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            string hotPoiName = hotPoiData?.PoiName ?? "Chưa có dữ liệu";

            // 4. Thời gian nghe trung bình (giây)
            var avgListenSeconds = await _context.PlaybackLogs
                .Where(l => l.DurationSeconds > 0)
                .AverageAsync(l => (double?)l.DurationSeconds) ?? 0;

            int avgListenTime = (int)Math.Round(avgListenSeconds);

            // 5. Danh sách người dùng đang hoạt động (top 8 gần nhất)
            var activeSessions = await _context.PlaybackLogs
                .Where(l => l.TriggeredAt >= now.AddMinutes(-5))
                .Include(l => l.POI)
                .OrderByDescending(l => l.TriggeredAt)
                .Take(8)
                .Select(l => new ActiveSession
                {
                    UserName = $"User #{l.UserId}",           // Cải tiến sau nếu có bảng User
                    CurrentPoi = l.POI != null ? l.POI.Name : "Không xác định",
                    ConnectedTime = GetRelativeTime(l.TriggeredAt)
                })
                .ToListAsync();

            return new DashboardDto
            {
                OnlineUsers = onlineUsers,
                ListeningUsers = listeningUsers,
                HotPoiName = hotPoiName,
                AvgListenTime = avgListenTime,
                ActiveSessions = activeSessions
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnalyticsService Error] {ex.Message}");

            // Trả về dữ liệu mặc định khi lỗi
            return new DashboardDto
            {
                OnlineUsers = 0,
                ListeningUsers = 0,
                HotPoiName = "Chưa có dữ liệu",
                AvgListenTime = 0,
                ActiveSessions = new List<ActiveSession>()
            };
        }
    }

    private string GetRelativeTime(DateTime triggeredAt)
    {
        var diff = DateTime.UtcNow - triggeredAt;
        if (diff.TotalMinutes < 1) return "Vừa xong";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
        return $"{(int)diff.TotalHours} giờ trước";
    }
}
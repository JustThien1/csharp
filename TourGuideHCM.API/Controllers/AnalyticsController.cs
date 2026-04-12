using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardDto>> GetDashboard()
        {
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-6); // 7 ngày gần nhất

            try
            {
                // Tổng POI & Users
                var totalPoi = await _context.POIs.CountAsync(p => p.IsActive);
                var totalUsers = await _context.Users.CountAsync();

                // Top POI - Tránh lỗi null propagating
                var topPoisQuery = await _context.PlaybackLogs
                    .Where(pl => pl.TriggeredAt >= startDate)
                    .Include(pl => pl.POI)                    // Load POI
                    .GroupBy(pl => pl.POIId)
                    .Select(g => new
                    {
                        POIId = g.Key,
                        Count = g.Count(),
                        PoiName = g.FirstOrDefault()!.POI != null
                                  ? g.FirstOrDefault()!.POI.Name
                                  : $"POI #{g.Key}"
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();

                var topPois = topPoisQuery.Select(x => new TopPoiDto
                {
                    Name = x.PoiName,
                    Count = x.Count
                }).ToList();

                var topPoiName = topPois.FirstOrDefault()?.Name ?? "Chưa có dữ liệu";

                // Thời gian nghe trung bình (phút)
                var avgListenSeconds = await _context.PlaybackLogs
                    .Where(pl => pl.DurationSeconds > 0)
                    .AverageAsync(pl => (double?)pl.DurationSeconds) ?? 0;

                var avgTimeMinutes = avgListenSeconds > 0 ? (int)Math.Round(avgListenSeconds / 60) : 5;

                // Daily Views 7 ngày gần nhất
                var dailyData = await _context.PlaybackLogs
                    .Where(pl => pl.TriggeredAt >= startDate)
                    .GroupBy(pl => pl.TriggeredAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();

                var dailyViews = new int[7];
                for (int i = 0; i < 7; i++)
                {
                    var currentDate = startDate.AddDays(i);
                    dailyViews[i] = dailyData.FirstOrDefault(x => x.Date == currentDate)?.Count ?? 0;
                }

                var dto = new DashboardDto
                {
                    TotalPoi = totalPoi,
                    TotalUsers = totalUsers,
                    TopPoi = topPoiName,
                    AvgTime = avgTimeMinutes,
                    TopPois = topPois,
                    DailyViews = dailyViews
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Analytics Error] {ex.Message}");
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }
    }
}
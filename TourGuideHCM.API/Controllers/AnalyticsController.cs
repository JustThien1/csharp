using Microsoft.AspNetCore.Mvc;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;        // ← Thêm using này

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
        public IActionResult GetDashboard()
        {
            var pois = _context.POIs.ToList();
            var users = _context.Users.ToList();

            var totalPoi = pois.Count;
            var totalUsers = users.Count;

            var topPoi = pois
                .OrderByDescending(p => p.Id)
                .Select(p => p.Name)
                .FirstOrDefault() ?? "N/A";

            var avgTime = 5;

            var rand = new Random();

            var topPois = pois
                .Take(5)
                .Select(p => new TopPoiDto
                {
                    Name = p.Name,
                    Count = rand.Next(50, 150)
                })
                .ToList();

            var dailyViews = new int[] { 5, 8, 12, 6, 10, 15, 20 };

            return Ok(new DashboardDto
            {
                TotalPoi = totalPoi,
                TotalUsers = totalUsers,
                TopPoi = topPoi,
                AvgTime = avgTime,
                TopPois = topPois,
                DailyViews = dailyViews
            });
        }
    }
}
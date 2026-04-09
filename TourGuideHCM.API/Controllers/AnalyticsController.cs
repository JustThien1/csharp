using Microsoft.AspNetCore.Mvc;
using TourGuideHCM.API.Data;

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

            var totalPoi = pois.Count;

            // 👉 Top POI (tạm lấy theo ID lớn nhất)
            var topPoi = pois
                .OrderByDescending(p => p.Id)
                .Select(p => p.Name)
                .FirstOrDefault() ?? "N/A";

            var avgTime = 5;

            // 👉 FIX: Random phải tạo 1 lần (tránh bug lặp số)
            var rand = new Random();

            var topPois = pois
                .Take(3)
                .Select(p => new
                {
                    name = p.Name,
                    count = rand.Next(50, 150)
                })
                .ToList();

            // 👉 Data chart
            var dailyViews = new int[] { 5, 8, 12, 6, 10, 15, 20 };

            return Ok(new
            {
                totalPoi,
                topPoi,
                avgTime,
                topPois,
                dailyViews
            });
        }
    }
}
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<POI> POIs { get; set; }
    }
}
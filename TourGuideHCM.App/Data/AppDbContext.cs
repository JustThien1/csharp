using Microsoft.EntityFrameworkCore;
using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Data;

public class AppDbContext : DbContext
{
    public DbSet<Poi> Pois { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Poi>().HasKey(p => p.Id);
    }
}
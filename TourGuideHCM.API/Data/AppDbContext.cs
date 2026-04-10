using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ====================== DbSets ======================
        public DbSet<POI> POIs { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<PlaybackLog> PlaybackLogs { get; set; }
        public DbSet<RouteLog> RouteLogs { get; set; }

        // ====================== THÊM DbSet cho Audio ======================
        public DbSet<Audio> Audios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<POI>()
                .HasOne(p => p.Category)
                .WithMany(c => c.POIs)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.POI)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.POIId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.POI)
                .WithMany(p => p.Favorites)
                .HasForeignKey(f => f.POIId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index cho Favorite
            modelBuilder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.POIId })
                .IsUnique();

            // ====================== RELATIONSHIP CHO AUDIO ======================
            modelBuilder.Entity<Audio>()
                .HasOne(a => a.POI)
                .WithMany(p => p.Audios)           // POI phải có ICollection<Audio> Audios
                .HasForeignKey(a => a.PoiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlaybackLog>()
                .HasOne(p => p.POI)
                .WithMany()
                .HasForeignKey(p => p.POIId)
                .OnDelete(DeleteBehavior.Cascade);

            // ====================== Seed Data ======================
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Di tích lịch sử" },
                new Category { Id = 2, Name = "Ẩm thực" },
                new Category { Id = 3, Name = "Mua sắm" }
            );
        }
    }
}
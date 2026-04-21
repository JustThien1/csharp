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
        public DbSet<Tour> Tours { get; set; }
        public DbSet<Audio> Audios { get; set; }
        public DbSet<PlaybackHistory> PlaybackHistories { get; set; } = null!;

        // ====================== MỚI ======================
        public DbSet<DuplicateReport> DuplicateReports { get; set; } = null!;
        public DbSet<TtsJob> TtsJobs { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.POIId })
                .IsUnique();

            modelBuilder.Entity<Audio>()
                .HasOne(a => a.POI)
                .WithMany(p => p.Audios)
                .HasForeignKey(a => a.PoiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlaybackLog>()
                .HasOne(p => p.POI)
                .WithMany()
                .HasForeignKey(p => p.POIId)
                .OnDelete(DeleteBehavior.Cascade);

            // ====================== DUPLICATE REPORT RELATIONSHIPS ======================
            // PoiA và PoiB đều là navigation đến bảng POIs, cần Restrict để không cascade delete
            modelBuilder.Entity<DuplicateReport>()
                .HasOne(r => r.PoiA)
                .WithMany()
                .HasForeignKey(r => r.PoiAId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DuplicateReport>()
                .HasOne(r => r.PoiB)
                .WithMany()
                .HasForeignKey(r => r.PoiBId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DuplicateReport>()
                .HasIndex(r => r.Status);

            // ====================== TTS JOB ======================
            modelBuilder.Entity<TtsJob>()
                .HasOne(j => j.POI)
                .WithMany()
                .HasForeignKey(j => j.PoiId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TtsJob>()
                .HasIndex(j => j.Status);

            // ====================== POI — Created By User ======================
            modelBuilder.Entity<POI>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);   // user bị xoá → POI vẫn còn, CreatedBy = null

            modelBuilder.Entity<POI>()
                .HasIndex(p => p.ReviewStatus);

            modelBuilder.Entity<POI>()
                .HasIndex(p => p.CreatedByUserId);

            // ====================== NOTIFICATION ======================
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            // User unique index
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // ====================== Seed Data ======================
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Di tích lịch sử" },
                new Category { Id = 2, Name = "Ẩm thực" },
                new Category { Id = 3, Name = "Mua sắm" }
            );
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Seed Categories
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Id = 1, Name = "Di tích lịch sử" },
                    new Category { Id = 2, Name = "Kiến trúc" },
                    new Category { Id = 3, Name = "Ẩm thực" }
                );
                context.SaveChanges();
            }

            // Seed POIs
            if (!context.POIs.Any())
            {
                context.POIs.AddRange(
                    new POI
                    {
                        Name = "Nhà thờ Đức Bà",
                        Description = "Nhà thờ Công giáo nổi tiếng tại TP.HCM",
                        Address = "1 Công trường Công xã Paris, Bến Nghé, Quận 1",
                        Lat = 10.779783,
                        Lng = 106.699018,
                        Radius = 100,
                        CategoryId = 1
                    },
                    new POI
                    {
                        Name = "Bưu điện Trung tâm TP.HCM",
                        Description = "Công trình kiến trúc Pháp cổ",
                        Address = "2 Công trường Công xã Paris, Bến Nghé, Quận 1",
                        Lat = 10.7805,
                        Lng = 106.6992,
                        Radius = 80,
                        CategoryId = 2
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
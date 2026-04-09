using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // ====================== SEED CATEGORIES ======================
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Id = 1, Name = "Di tích lịch sử", Icon = "🏛️" },
                    new Category { Id = 2, Name = "Kiến trúc Pháp", Icon = "🏰" },
                    new Category { Id = 3, Name = "Ẩm thực", Icon = "🍜" },
                    new Category { Id = 4, Name = "Mua sắm & Giải trí", Icon = "🛍️" }
                );
                context.SaveChanges();
                Console.WriteLine("✅ Đã seed Categories");
            }

            // ====================== SEED POIs ======================
            if (!context.POIs.Any())
            {
                context.POIs.AddRange(
     new POI
     {
         Name = "Nhà thờ Đức Bà",
         Description = "Nhà thờ Công giáo nổi tiếng và biểu tượng của TP. Hồ Chí Minh",
         Address = "1 Công trường Công xã Paris, Bến Nghé, Quận 1",
         Lat = 10.779783,
         Lng = 106.699018,
         Radius = 120,
         AudioUrl = "audio/nhatho.mp3",
         NarrationText = "Nhà thờ Đức Bà là một trong những công trình kiến trúc đẹp nhất Sài Gòn, được xây dựng từ năm 1863.",
         Priority = 1,
         IsActive = true,
         CategoryId = 1,
         OpeningHours = "05:00 - 22:00",
         TicketPrice = 0,
         ImageUrl = "https://lh3.googleusercontent.com/gps-cs-s/AHVAweqptCrhOHhBUa5925DwRvrtyLlIC-wMFLRnEGWJV6iUt2BwCOeu0SWHuoQO4Qph_LkVWtaLqm104YrtUoJ4_orVsjowX9IwHSL3WDBEOtgsBFy7INDH04W8RJiNlGlDBUU3z0-YJw=w270-h312-n-k-no"
     },
     new POI
     {
         Name = "Bưu điện Trung tâm TP.HCM",
         Description = "Công trình kiến trúc Pháp cổ kính",
         Address = "2 Công trường Công xã Paris, Bến Nghé, Quận 1",
         Lat = 10.7805,
         Lng = 106.6992,
         Radius = 90,
         AudioUrl = "audio/buudien.mp3",
         NarrationText = "Bưu điện Trung tâm được xây dựng vào cuối thế kỷ 19.",
         Priority = 2,
         IsActive = true,
         CategoryId = 2,
         OpeningHours = "07:00 - 20:00",
         TicketPrice = 0,
         ImageUrl = "https://dynamic-media-cdn.tripadvisor.com/media/photo-o/2e/8c/e2/12/caption.jpg?w=900&h=500&s=1"
     },
     new POI
     {
         Name = "Chợ Bến Thành",
         Description = "Chợ truyền thống nổi tiếng nhất Sài Gòn",
         Address = "Bến Thành, Quận 1",
         Lat = 10.7715,
         Lng = 106.6980,
         Radius = 150,
         AudioUrl = "audio/benthanh.mp3",
         NarrationText = "Chợ Bến Thành là nơi mua sắm và ẩm thực đặc trưng.",
         Priority = 3,
         IsActive = true,
         CategoryId = 4,
         OpeningHours = "06:00 - 23:00",
         TicketPrice = 0,
         ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/9/91/Ben_Thanh_market_2.jpg"
     },
     new POI
     {
         Name = "Bitexco Financial Tower",
         Description = "Tòa nhà cao nhất TP.HCM một thời",
         Address = "2 Hải Triều, Bến Nghé, Quận 1",
         Lat = 10.7718,
         Lng = 106.7042,
         Radius = 100,
         AudioUrl = "audio/bitexco.mp3",
         NarrationText = "Bitexco cao 68 tầng, biểu tượng hiện đại.",
         Priority = 4,
         IsActive = true,
         CategoryId = 2,
         OpeningHours = "09:00 - 22:00",
         TicketPrice = 200000,
         ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTBOTMhdc1RTNgRi5BCHeX8wKcot-3_HjGjgA&s"
     }
 );

                context.SaveChanges();
                Console.WriteLine("✅ Đã seed 4 địa điểm POI mẫu");
            }
            else
            {
                Console.WriteLine($"ℹ️ Đã có {context.POIs.Count()} POI trong database");
            }
        }
    }
}
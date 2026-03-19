using TourGuideHCM.API.Models;

namespace TourGuideHCM.API.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            if (context.POIs.Any()) return;

            context.POIs.Add(new POI
            {
                Name = "Nhà thờ Đức Bà",
                Description = "Địa điểm nổi tiếng",
                Lat = 10.779783,
                Lng = 106.699018
            });

            context.SaveChanges();
        }
    }
}
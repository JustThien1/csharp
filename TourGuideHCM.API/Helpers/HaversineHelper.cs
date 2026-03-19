namespace TourGuideHCM.API.Helpers
{
    public static class HaversineHelper
    {
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371;

            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c * 1000;
        }

        private static double ToRad(double val)
        {
            return val * Math.PI / 180;
        }
    }
}
namespace TourGuideHCM.API.Models
{
    public class POI
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Radius { get; set; }
        public string AudioUrl { get; set; }

    }
}
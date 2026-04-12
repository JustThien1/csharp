namespace TourGuideHCM.Admin.Models
{
    public class PoiDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Address { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string ImageUrl { get; set; } = "";
        public int CategoryId { get; set; } = 1;
    }
}
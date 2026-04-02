namespace TourGuideHCM.API.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }

        public ICollection<POI> POIs { get; set; } = new List<POI>();
    }
}
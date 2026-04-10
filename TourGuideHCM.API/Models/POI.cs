namespace TourGuideHCM.API.Models
{
    public class POI
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Radius { get; set; } = 100;           // mét
        public int Priority { get; set; } = 1;              // ưu tiên (cao hơn trigger trước)

        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }               // file thu sẵn
        public string? NarrationText { get; set; }          // text cho TTS
        public string Language { get; set; } = "vi";        // vi, en, zh...

        public string? OpeningHours { get; set; }
        public decimal? TicketPrice { get; set; }
        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<Audio> Audios { get; set; } = new List<Audio>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}
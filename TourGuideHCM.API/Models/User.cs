namespace TourGuideHCM.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}
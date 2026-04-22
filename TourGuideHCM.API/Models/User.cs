namespace TourGuideHCM.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;   // BCrypt hash
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        /// <summary>"Admin" | "Saler" — quyết định quyền trong hệ thống.</summary>
        public string Role { get; set; } = "Saler";

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }

        /// <summary>UTC — Saler còn quyền dùng app đến thời điểm này (sau đó cần gia hạn).</summary>
        public DateTime? SubscriptionEndUtc { get; set; }

        // Navigation properties
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}

namespace TourGuideHCM.API.Models
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }  // ← Thêm SĐT
    }
}

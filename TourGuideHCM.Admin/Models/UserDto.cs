namespace TourGuideHCM.Admin.Models
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Role { get; set; } = "User";        // User, Admin, Guide
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int TotalListens { get; set; }             // Số lượt nghe POI
    }
}
namespace TourGuideHCM.Admin.Models
{
    public class AudioDto
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public string PoiName { get; set; } = "";
        public string Language { get; set; } = "vi";
        public string AudioUrl { get; set; } = "";
        public int DurationSeconds { get; set; }
        public string Description { get; set; } = "";
        public bool IsActive { get; set; } = true;

        // Trường hỗ trợ upload (chỉ dùng ở client)
        public string? FileName { get; set; }
    }
}
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

        /// <summary>
        /// Trạng thái duyệt POI:
        /// - "Approved": đã duyệt, hiển thị cho user app bình thường (mặc định khi admin tạo)
        /// - "PendingReview": chờ admin duyệt (saler tạo mới hoặc sửa)
        /// - "Rejected": admin từ chối (lý do ở RejectionReason)
        /// - "Locked": đã approved rồi nhưng admin khoá lại (ẩn khỏi user app)
        /// </summary>
        public string ReviewStatus { get; set; } = "Approved";

        /// <summary>Lý do admin reject/lock POI — hiển thị cho saler trong notification.</summary>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// ID của user (saler hoặc admin) đã tạo POI này.
        /// NULL nghĩa là POI cũ có từ trước khi có hệ thống saler.
        /// </summary>
        public int? CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedByUserId { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<Audio> Audios { get; set; } = new List<Audio>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}

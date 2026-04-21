using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class POIController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly POIService _service;
        private readonly GeofenceService _geofenceService;
        private readonly DuplicateDetectionService _duplicateDetector;
        private readonly CurrentUserService _currentUser;

        public POIController(
            AppDbContext context,
            POIService service,
            GeofenceService geofenceService,
            DuplicateDetectionService duplicateDetector,
            CurrentUserService currentUser)
        {
            _context = context;
            _service = service;
            _geofenceService = geofenceService;
            _duplicateDetector = duplicateDetector;
            _currentUser = currentUser;
        }

        // ✅ GET ALL — public endpoint cho App MAUI và Admin
        // User app: chỉ thấy POI đã Approved
        // Admin panel: thấy tất cả
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var query = _context.POIs
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            // Nếu là admin hoặc chưa login → admin panel cũ vẫn dùng như cũ
            // Chỉ lọc Approved khi rõ ràng là anonymous user (app MAUI)
            // → Đơn giản: admin panel sẽ thấy hết, user app chỉ thấy Approved
            if (!_currentUser.IsAdmin)
            {
                query = query.Where(p => p.ReviewStatus == "Approved");
            }

            var list = await query.OrderBy(p => p.Priority).ToListAsync();
            return Ok(list);
        }

        // ✅ GET BY ID
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var poi = _service.GetById(id);
            return poi == null ? NotFound("Không tìm thấy POI") : Ok(poi);
        }

        // ====================== MỚI: POI của saler hiện tại ======================
        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUser.UserId;
            if (userId == 0) return Unauthorized();

            var list = await _context.POIs
                .Include(p => p.Category)
                .Where(p => p.CreatedByUserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(list);
        }

        // ✅ CREATE — tự set CreatedByUserId + ReviewStatus theo role
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] POI poi)
        {
            if (poi == null || string.IsNullOrWhiteSpace(poi.Name))
                return BadRequest("Tên POI không hợp lệ");

            // Nếu đã login → gán CreatedByUserId
            if (_currentUser.IsAuthenticated && _currentUser.UserId > 0)
            {
                poi.CreatedByUserId = _currentUser.UserId;

                // Saler: POI tạo mới phải chờ duyệt
                // Admin: POI được duyệt luôn
                poi.ReviewStatus = _currentUser.IsAdmin ? "Approved" : "PendingReview";
                poi.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // Anonymous / admin panel cũ (chưa login) → giữ hành vi cũ (Approved)
                poi.ReviewStatus = "Approved";
                poi.CreatedAt = DateTime.UtcNow;
            }

            var created = _service.Add(poi);

            // Detect duplicate (không chặn)
            var duplicates = await _duplicateDetector.CheckAndReportAsync(created.Id);

            // Nếu là saler → gửi notification cho tất cả admin
            if (_currentUser.IsSaler)
            {
                await NotifyAdminsNewPoi(created);
            }

            return CreatedAtAction(nameof(GetById),
                new { id = created.Id },
                new
                {
                    poi = created,
                    hasDuplicateWarning = duplicates.Any(),
                    duplicates = duplicates.Select(d => new
                    {
                        existingId = d.ExistingPoi.Id,
                        existingName = d.ExistingPoi.Name,
                        level = d.Level,
                        similarity = d.NameSimilarity,
                        distance = d.DistanceMeters
                    })
                });
        }

        // ✅ UPDATE — chỉ cho saler sửa POI của mình; sửa xong về PendingReview
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] POI updated)
        {
            if (updated == null) return BadRequest("Dữ liệu không hợp lệ");

            var existing = await _context.POIs.FindAsync(id);
            if (existing == null) return NotFound("Không tìm thấy POI");

            // Kiểm tra quyền
            if (_currentUser.IsSaler)
            {
                // Saler chỉ được sửa POI của mình
                if (existing.CreatedByUserId != _currentUser.UserId)
                    return Forbid();

                // Không sửa được POI đang bị Locked
                if (existing.ReviewStatus == "Locked")
                    return BadRequest(new { message = "POI đã bị khoá. Liên hệ admin để mở khoá." });
            }

            // Copy field (trừ các field về trạng thái duyệt)
            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.Address = updated.Address;
            existing.Lat = updated.Lat;
            existing.Lng = updated.Lng;
            existing.Radius = updated.Radius;
            existing.Priority = updated.Priority;
            existing.ImageUrl = updated.ImageUrl;
            existing.NarrationText = updated.NarrationText;
            existing.Language = updated.Language;
            existing.OpeningHours = updated.OpeningHours;
            existing.TicketPrice = updated.TicketPrice;
            existing.CategoryId = updated.CategoryId;

            // Saler sửa POI đã duyệt → chuyển về PendingReview
            if (_currentUser.IsSaler && existing.ReviewStatus == "Approved")
            {
                existing.ReviewStatus = "PendingReview";
                await NotifyAdminsPoiEdited(existing);
            }

            // Admin sửa POI của saler → notify saler biết
            if (_currentUser.IsAdmin
                && existing.CreatedByUserId.HasValue
                && existing.CreatedByUserId.Value != _currentUser.UserId)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = existing.CreatedByUserId.Value,
                    Type = "PoiEdited",
                    Title = "POI của bạn đã được admin chỉnh sửa",
                    Message = $"Admin vừa chỉnh sửa thông tin POI '{existing.Name}' của bạn.",
                    RelatedPoiId = existing.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // Re-check duplicate
            var duplicates = await _duplicateDetector.CheckAndReportAsync(id);

            return Ok(new
            {
                poi = existing,
                hasDuplicateWarning = duplicates.Any()
            });
        }

        // ✅ DELETE — saler xoá POI của mình; admin xoá được tất cả (notify saler)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.POIs.FindAsync(id);
            if (existing == null) return NotFound("Không tìm thấy POI");

            if (_currentUser.IsSaler && existing.CreatedByUserId != _currentUser.UserId)
                return Forbid();

            // Admin xoá POI của saler khác → notify saler TRƯỚC khi xoá
            // (vì sau khi xoá POI, RelatedPoiId sẽ trỏ tới ID không còn tồn tại — vẫn OK vì FK chỉ SET NULL)
            if (_currentUser.IsAdmin
                && existing.CreatedByUserId.HasValue
                && existing.CreatedByUserId.Value != _currentUser.UserId)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = existing.CreatedByUserId.Value,
                    Type = "PoiDeleted",
                    Title = "POI của bạn đã bị admin xoá",
                    Message = $"Admin đã xoá POI '{existing.Name}'. Audio liên quan cũng bị xoá.",
                    RelatedPoiId = null,     // POI sắp bị xoá → không để RelatedPoiId
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            var result = _service.Delete(id);
            return !result
                ? NotFound("Không tìm thấy POI")
                : Ok(new { message = "Xóa thành công" });
        }

        // 🔥 NEARBY
        [HttpGet("nearby")]
        public IActionResult GetNearby([FromQuery] double lat, [FromQuery] double lng)
        {
            if (lat == 0 || lng == 0)
                return BadRequest("Vui lòng truyền lat và lng");

            var pois = _service.GetAll().Where(p => p.ReviewStatus == "Approved");
            var nearest = _geofenceService.GetNearestPOI(lat, lng, pois.ToList());

            return nearest == null
                ? Ok(new { message = "Không tìm thấy POI gần" })
                : Ok(nearest);
        }

        // 🔥 GEOFENCE TRIGGER
        [HttpPost("trigger")]
        public IActionResult TriggerGeofence([FromBody] LocationRequest request)
        {
            if (request == null || request.Lat == 0 || request.Lng == 0)
                return BadRequest("Tọa độ không hợp lệ");

            var poi = _geofenceService.GetTriggeredPOI(request.Lat, request.Lng, 0);

            if (poi == null || poi.ReviewStatus != "Approved")
                return Ok(new { triggered = false });

            return Ok(new
            {
                triggered = true,
                poiId = poi.Id,
                poiName = poi.Name,
                audioUrl = poi.AudioUrl,
                narrationText = poi.NarrationText
            });
        }

        // 🔥 QR
        [HttpGet("qr/{code}")]
        public IActionResult GetByQRCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("QR Code không hợp lệ");

            var poi = _service.GetByQRCode(code);
            return poi == null ? NotFound("Không tìm thấy POI") : Ok(poi);
        }

        // ====================== NOTIFICATION HELPERS ======================
        private async Task NotifyAdminsNewPoi(POI poi)
        {
            var admins = await _context.Users
                .Where(u => u.Role == "Admin" && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var adminId in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = adminId,
                    Type = "PoiCreated",
                    Title = "POI mới chờ duyệt",
                    Message = $"Saler '{_currentUser.Username}' vừa tạo POI '{poi.Name}'",
                    RelatedPoiId = poi.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task NotifyAdminsPoiEdited(POI poi)
        {
            var admins = await _context.Users
                .Where(u => u.Role == "Admin" && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var adminId in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = adminId,
                    Type = "PoiCreated",
                    Title = "POI đã chỉnh sửa",
                    Message = $"Saler '{_currentUser.Username}' vừa sửa POI '{poi.Name}' — cần duyệt lại",
                    RelatedPoiId = poi.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }
    }

    public class LocationRequest
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}

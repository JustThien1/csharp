using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourGuideHCM.API.Models;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class POIController : ControllerBase
    {
        private readonly POIService _service;
        private readonly GeofenceService _geofenceService;

        public POIController(POIService service, GeofenceService geofenceService)
        {
            _service = service;
            _geofenceService = geofenceService;
        }

        // ✅ GET ALL
        [HttpGet]
        public IActionResult GetAll()
            => Ok(_service.GetAll());

        // ✅ GET BY ID
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var poi = _service.GetById(id);
            return poi == null ? NotFound("Không tìm thấy POI") : Ok(poi);
        }

        // ✅ CREATE (⚠️ bỏ Authorize để Admin dùng)
        [HttpPost]
        public IActionResult Create([FromBody] POI poi)
        {
            if (poi == null || string.IsNullOrWhiteSpace(poi.Name))
                return BadRequest("Tên POI không hợp lệ");

            var created = _service.Add(poi);

            return CreatedAtAction(nameof(GetById),
                new { id = created.Id },
                created);
        }

        // ✅ UPDATE
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] POI updated)
        {
            if (updated == null)
                return BadRequest("Dữ liệu không hợp lệ");

            var result = _service.Update(id, updated);

            return !result
                ? NotFound("Không tìm thấy POI")
                : Ok(updated);
        }

        // ✅ DELETE
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
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

            var pois = _service.GetAll();
            var nearest = _geofenceService.GetNearestPOI(lat, lng, pois);

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

            var userId = GetCurrentUserId();

            var poi = _geofenceService.GetTriggeredPOI(request.Lat, request.Lng, userId);

            if (poi == null)
                return Ok(new { triggered = false });

            _service.LogPlayback(userId, poi.Id, "geofence");

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

            return poi == null
                ? NotFound("Không tìm thấy POI")
                : Ok(poi);
        }

        // 🔧 HELPER
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("id")?.Value;

            return int.TryParse(claim, out int id) ? id : 0;
        }
    }

    public class LocationRequest
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
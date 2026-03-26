using Microsoft.AspNetCore.Mvc;
using TourGuideHCM.API.Models;
using TourGuideHCM.API.Services;

namespace TourGuideHCM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class POIController : ControllerBase
    {
        private readonly POIService _service;

        public POIController(POIService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_service.GetAll());
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var poi = _service.GetById(id);
            if (poi == null) return NotFound();
            return Ok(poi);
        }

        [HttpPost]
        public IActionResult Create(POI poi)
        {
            _service.Add(poi);
            return Ok(poi);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, POI updated)
        {
            var result = _service.Update(id, updated);
            if (!result) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var result = _service.Delete(id);
            if (!result) return NotFound();
            return Ok();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
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
    }
}
using Microsoft.AspNetCore.Mvc;

namespace FindIFBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        [HttpHead]
        public IActionResult Get() => Ok("FindIFBot is healthy!");
    }
}

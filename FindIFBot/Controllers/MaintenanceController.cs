using FindIFBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace FindIFBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        [HttpGet("process-yesterday-logs")]
        public async Task<IActionResult> ProcessYesterdayLogs()
        {
            await _maintenanceService.ProcessYesterdayLogsAsync();

            return Ok(new { message = "Yesterday's logs processed and sent to Telegram channel" });
        }
    }
}

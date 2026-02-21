using System.Security.Cryptography;
using System.Text;
using FindIFBot.Configuration;
using FindIFBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace FindIFBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly MaintenanceOptions _options;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(
            IMaintenanceService maintenanceService,
            IOptions<MaintenanceOptions> options,
            ILogger<MaintenanceController> logger)
        {
            _maintenanceService = maintenanceService;
            _options = options.Value;
            _logger = logger;
        }

        [HttpPost("process-yesterday-logs")]
        [EnableRateLimiting("maintenance")]
        public async Task<IActionResult> ProcessYesterdayLogs([FromHeader(Name = "X-Maintenance-Key")] string key,
            CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateMaintenanceKey(key);
            if (validationResult != null)
                return validationResult;

            _logger.LogInformation("Starting daily log maintenance job");

            try
            {
                await _maintenanceService.ProcessYesterdayLogsAsync(cancellationToken);

                _logger.LogInformation("Daily log maintenance completed successfully");

                return Ok(new { message = "Yesterday's logs processed and sent to Telegram channel" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during daily log maintenance");

                return StatusCode(500, new { error = "Internal server error during processing" });
            }
        }

        [HttpPost("daily-statistics")]
        [EnableRateLimiting("maintenance")]
        public async Task<IActionResult> GenerateDailyStatistics([FromHeader(Name = "X-Maintenance-Key")] string key,
            CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateMaintenanceKey(key);
            if (validationResult != null)
                return validationResult;

            _logger.LogInformation("Starting daily statistics job");

            try
            {
                await _maintenanceService.SendDailyStatisticsAsync(cancellationToken);

                _logger.LogInformation("Daily statistics job completed successfully");

                return Ok(new { message = "Daily statistics processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during daily statistics job");

                return StatusCode(500, new { error = "Internal server error during processing" });
            }
        }

        /// <summary>
        /// Validates the maintenance key using constant-time comparison (prevents timing attacks)
        /// Returns Unauthorized result if invalid, otherwise null (success).
        /// </summary>
        private IActionResult? ValidateMaintenanceKey(string key)
        {
            if (string.IsNullOrEmpty(key) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(key),
                    Encoding.UTF8.GetBytes(_options.SecretKey)))
            {
                _logger.LogWarning("Unauthorized maintenance attempt from IP: {IP}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                return Unauthorized(new { error = "Invalid or missing maintenance key" });
            }

            return null;
        }
    }
}

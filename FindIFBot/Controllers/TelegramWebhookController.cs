using FindIFBot.Helpers.Logs;
using FindIFBot.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace FindIFBot.Controllers
{
    [ApiController]
    [Route("api/telegram/webhook")]
    public class TelegramWebhookController : ControllerBase
    {
        private readonly ICommandDispatcher _dispatcher;
        private readonly IAppLogger<TelegramWebhookController> _logger;

        private const string Component = "TelegramWebhookController";

        public TelegramWebhookController(ICommandDispatcher dispatcher, 
            IAppLogger<TelegramWebhookController> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update == null)
                return Ok();

            try
            {
                await _dispatcher.DispatchAsync(update);
            }
            catch (Exception ex)
            {
                // Log but always return 200 — prevents Telegram from retrying the same update
                _ = _logger.LogError(Component, $"Unhandled exception in DispatchAsync. UpdateId: {update.Id}," +
                    $"Error: {ex.Message}");
            }

            return Ok();
        }
    }
}

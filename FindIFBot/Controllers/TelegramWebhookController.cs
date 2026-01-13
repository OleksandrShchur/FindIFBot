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

        public TelegramWebhookController(ICommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update == null)
                return Ok();

            await _dispatcher.DispatchAsync(update);

            return Ok();
        }
    }
}

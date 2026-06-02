using FindIFBot.Configuration;
using FindIFBot.Helpers.Logs;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Admin
{
    public class CallbackAuthorizationService : ICallbackAuthorizationService
    {
        private const string Component = "AdminAuthorization";

        private readonly IAppLogger<CallbackAuthorizationService> _logger;
        private readonly TelegramOptions _options;

        public CallbackAuthorizationService(
            IAppLogger<CallbackAuthorizationService> logger,
            IOptions<TelegramOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task<bool> IsAuthorizedAsync(CallbackQuery callback, AdminCallbackData data)
        {
            if (data.IsUserAction)
            {
                if (callback.From.Id == data.UserId)
                {
                    return true;
                }

                await _logger.LogWarning(Component,
                    $"Invalid sender for user callback | Expected: {data.UserId} | Actual: {callback.From.Id}");
                return false;
            }

            if (callback.From.Id == _options.AdminId)
            {
                return true;
            }

            await _logger.LogWarning(Component,
                $"Invalid sender for admin callback | Expected: {_options.AdminId} | Actual: {callback.From.Id}");
            return false;
        }
    }
}

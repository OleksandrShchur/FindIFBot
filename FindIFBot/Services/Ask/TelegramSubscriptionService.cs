using FindIFBot.Configuration;
using FindIFBot.Helpers.Logs;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Services.Ask
{
    public class TelegramSubscriptionService : ISubscriptionService
    {
        private const string Component = "Subscription";

        private readonly ITelegramBotClient _bot;
        private readonly IAppLogger<TelegramSubscriptionService> _logger;
        private readonly TelegramOptions _options;

        public TelegramSubscriptionService(
            ITelegramBotClient bot,
            IAppLogger<TelegramSubscriptionService> logger,
            IOptions<TelegramOptions> options)
        {
            _bot = bot;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<bool> IsSubscribedToOutputChannelAsync(long userId)
        {
            try
            {
                var member = await _bot.GetChatMember(_options.UserOutputChannel, userId);

                return member switch
                {
                    ChatMemberMember => true,
                    ChatMemberAdministrator => true,
                    ChatMemberOwner => true,
                    ChatMemberRestricted restricted => restricted.CanSendMessages,
                    _ => false
                };
            }
            catch (ApiRequestException ex)
            {
                await _logger.LogWarning(Component,
                    $"Failed to check output channel membership | UserId: {userId} | ErrorCode: {ex.ErrorCode} | Message: {ex.Message}");

                return false;
            }
        }
    }
}

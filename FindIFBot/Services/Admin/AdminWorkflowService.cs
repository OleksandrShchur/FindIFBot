using FindIFBot.Domain;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers.Logs;
using FindIFBot.Persistence;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Admin
{
    public class AdminWorkflowService : IAdminWorkflowService
    {
        private const string Component = "AdminWorkflow";

        private readonly ITelegramBotClient _bot;
        private readonly IMessageStore _messages;
        private readonly IUserSessionRepository _sessions;
        private readonly IAdminCallbackParser _parser;
        private readonly ICallbackAuthorizationService _authorization;
        private readonly IAdminModerationService _moderation;
        private readonly IAppLogger<AdminWorkflowService> _logger;

        public AdminWorkflowService(
            ITelegramBotClient bot,
            IMessageStore messages,
            IUserSessionRepository sessions,
            IAdminCallbackParser parser,
            ICallbackAuthorizationService authorization,
            IAdminModerationService moderation,
            IAppLogger<AdminWorkflowService> logger)
        {
            _bot = bot;
            _messages = messages;
            _sessions = sessions;
            _parser = parser;
            _authorization = authorization;
            _moderation = moderation;
            _logger = logger;
        }

        public async Task HandleCallbackAsync(CallbackQuery callback)
        {
            if (!_parser.TryParse(callback.Data, out var data))
            {
                return;
            }

            await _logger.LogInfo(Component,
                $"Callback received | Action: {data.Action} | UserId: {data.UserId} | MessageId: {data.MessageId} | FromUserId: {callback.From.Id}");

            if (!await _authorization.IsAuthorizedAsync(callback, data))
            {
                return;
            }

            if (await _messages.TryGetAsync(data.MessageId) is not { } stored)
            {
                await _logger.LogError(Component,
                    $"Stored message not found on callback | UserId: {data.UserId} | MessageId: {data.MessageId}");
                return;
            }

            await _bot.AnswerCallbackQuery(callback.Id);

            switch (data.Action)
            {
                case "+ask":
                    await _logger.LogInfo(Component,
                        $"Admin approved ask request | UserId: {data.UserId} | MessageId: {data.MessageId}");
                    await _moderation.PublishAsync(data.UserId, stored);
                    await CleanupAsync(callback, data.MessageId);
                    return;
                case "-ask":
                    await _logger.LogInfo(Component,
                        $"Admin rejected ask request | UserId: {data.UserId} | MessageId: {data.MessageId}");
                    await _moderation.RejectAsync(data.UserId, data.MessageId);
                    await CleanupAsync(callback, data.MessageId);
                    return;
                case "?ask":
                    await _logger.LogInfo(Component,
                        $"Admin marked ask as duplicate | UserId: {data.UserId} | MessageId: {data.MessageId}");
                    await _moderation.MarkDuplicateAsync(data.UserId, data.MessageId);
                    await CleanupAsync(callback, data.MessageId);
                    return;
                case "!ask":
                    await _logger.LogInfo(Component,
                        $"Admin marked ask as advertisement | UserId: {data.UserId} | MessageId: {data.MessageId}");
                    await _moderation.MarkAdvertisementAsync(data.UserId, data.MessageId);
                    await CleanupAsync(callback, data.MessageId);
                    return;
                case "*ask":
                    await _logger.LogInfo(Component,
                        $"Admin marked ask as needs attention | UserId: {data.UserId} | MessageId: {data.MessageId}");
                    await _moderation.MarkNeedsAttentionAsync(data.UserId, data.MessageId);
                    await CleanupAsync(callback, data.MessageId);
                    return;
                case "proceed":
                    await HandleProceedAsync(callback, data, stored);
                    return;
                case "cancel":
                    await _logger.LogInfo(Component,
                        $"User cancelled submission | UserId: {data.UserId} | MessageId: {data.MessageId}");
                    await _moderation.CancelAskAsync(data.UserId, data.MessageId);
                    await CleanupAsync(callback, data.MessageId);
                    return;
            }
        }

        public Task SubmitAskAsync(StoredMessage stored, UserInfo userInfo) =>
            _moderation.SubmitAskAsync(stored, userInfo);

        private async Task HandleProceedAsync(
            CallbackQuery callback,
            AdminCallbackData data,
            StoredMessage stored)
        {
            await _logger.LogInfo(Component,
                $"User confirmed submission | UserId: {data.UserId} | MessageId: {data.MessageId}");

            await _moderation.SubmitAskAsync(stored, CreateUserInfo(callback.From));

            var session = await _sessions.GetAsync(data.UserId);
            session.State = UserState.Idle;
            await _sessions.SaveAsync(session);

            try
            {
                await _bot.DeleteMessage(callback.Message!.Chat.Id, callback.Message.MessageId);
            }
            catch
            {
            }
        }

        private async Task CleanupAsync(CallbackQuery callback, int messageId)
        {
            await _messages.RemoveAsync(messageId);
            await _logger.LogInfo(Component, $"Cleanup: removed stored message | MessageId: {messageId}");

            try
            {
                await _bot.DeleteMessage(callback.Message!.Chat.Id, callback.Message.MessageId);
            }
            catch
            {
            }
        }

        private static UserInfo CreateUserInfo(User user) =>
            new()
            {
                Id = user.Id,
                UserName = user.Username ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsBot = user.IsBot,
                LanguageCode = user.LanguageCode ?? string.Empty,
                IsPremium = user.IsPremium
            };
    }
}

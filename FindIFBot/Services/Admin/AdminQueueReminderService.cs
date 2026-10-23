using FindIFBot.Configuration;
using FindIFBot.EF.Repositories;
using FindIFBot.Helpers;
using FindIFBot.Helpers.Logs;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Services.Admin
{
    public interface IAdminQueueReminderService
    {
        /// <summary>
        /// If a reminder is due during working hours and the queue is non-empty, sends one
        /// reminder about the oldest pending request and reschedules.
        /// </summary>
        Task ProcessDueAsync(CancellationToken cancellationToken = default);
    }

    public class AdminQueueReminderService : IAdminQueueReminderService
    {
        private const string Component = "AdminQueueReminder";
        private static readonly LinkPreviewOptions NoPreview = new() { IsDisabled = true };

        private readonly IAdminQueueReminderStateRepository _state;
        private readonly IUserRequestHistoryRepository _history;
        private readonly ITelegramBotClient _bot;
        private readonly TelegramOptions _telegram;
        private readonly ReminderOptions _reminder;
        private readonly IWorkingHours _workingHours;
        private readonly TimeProvider _timeProvider;
        private readonly IAppLogger<AdminQueueReminderService> _logger;

        public AdminQueueReminderService(
            IAdminQueueReminderStateRepository state,
            IUserRequestHistoryRepository history,
            ITelegramBotClient bot,
            IOptions<TelegramOptions> telegram,
            IOptions<ReminderOptions> reminder,
            IWorkingHours workingHours,
            TimeProvider timeProvider,
            IAppLogger<AdminQueueReminderService> logger)
        {
            _state = state;
            _history = history;
            _bot = bot;
            _telegram = telegram.Value;
            _reminder = reminder.Value;
            _workingHours = workingHours;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task ProcessDueAsync(CancellationToken cancellationToken = default)
        {
            var now = _timeProvider.GetUtcNow();
            var existing = await _state.GetAsync(cancellationToken);
            if (existing?.DueAtUtc is null || existing.DueAtUtc > now.UtcDateTime)
                return;

            var oldest = await _history.GetOldestPendingAsync(cancellationToken);
            if (oldest is null)
            {
                await _state.ClearAsync(cancellationToken);
                return;
            }

            if (!_workingHours.IsWorkingHours(now))
                return;

            await SendReminderAsync(oldest.UserMessageId, oldest.AdminInfoMessageId, cancellationToken);

            var nextDue = now.UtcDateTime.Add(TimeSpan.FromMinutes(Math.Max(1, _reminder.DelayMinutes)));
            await _state.UpsertAsync(nextDue, now.UtcDateTime, cancellationToken);

            await _logger.LogInfo(Component,
                $"Reminder sent for oldest pending | MessageId: {oldest.UserMessageId} | NextDueUtc: {nextDue:O}");
        }

        private async Task SendReminderAsync(
            int userMessageId,
            int? adminInfoMessageId,
            CancellationToken cancellationToken)
        {
            var text = $"⏳ Нагадування: запит #<code>{userMessageId}</code> очікує дії адміна.";

            if (adminInfoMessageId is int replyTo)
            {
                try
                {
                    await _bot.SendMessage(
                        chatId: _telegram.AdminId,
                        text: text,
                        replyParameters: new ReplyParameters { MessageId = replyTo },
                        linkPreviewOptions: NoPreview,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                    return;
                }
                catch (ApiRequestException ex) when (
                    ex.ErrorCode == 400 &&
                    ex.Message?.Contains("reply message not found") == true)
                {
                    await _bot.SendMessage(
                        chatId: _telegram.AdminId,
                        text: text + "\n\n<i>(Оригінальне повідомлення з інформацією про користувача не знайдено)</i>",
                        linkPreviewOptions: NoPreview,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);
                    return;
                }
            }

            await _bot.SendMessage(
                chatId: _telegram.AdminId,
                text: text + "\n\n<i>(Посилання на тред модерації відсутнє для цього запиту)</i>",
                linkPreviewOptions: NoPreview,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
    }
}

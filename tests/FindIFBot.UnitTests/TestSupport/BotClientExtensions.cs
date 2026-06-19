using NSubstitute.Core;
using Telegram.Bot;

namespace FindIFBot.UnitTests.TestSupport
{
    /// <summary>
    /// All Telegram extension methods (SendMessage, AnswerCallbackQuery, ...) funnel through
    /// the single core interface method <see cref="ITelegramBotClient.SendRequest{TResponse}"/>.
    /// These helpers extract the strongly-typed request objects that were sent so tests can
    /// assert on real behavior (target chat, text, reply markup) instead of opaque calls.
    /// </summary>
    internal static class BotClientExtensions
    {
        public static IReadOnlyList<T> SentRequests<T>(this ITelegramBotClient bot) where T : class =>
            bot.ReceivedCalls()
                .Where(c => c.GetMethodInfo().Name == nameof(ITelegramBotClient.SendRequest))
                .Select(c => c.GetArguments()[0])
                .OfType<T>()
                .ToList();

        public static T SingleRequest<T>(this ITelegramBotClient bot) where T : class =>
            bot.SentRequests<T>().Single();
    }
}

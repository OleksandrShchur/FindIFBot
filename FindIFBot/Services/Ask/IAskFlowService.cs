using FindIFBot.EF.Entities;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Ask
{
    public interface IAskFlowService
    {
        Task StartAsync(long chatId, long userId, UserSession session);
        Task HandleCallbackAsync(CallbackQuery callback);
    }
}

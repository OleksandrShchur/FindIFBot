using FindIFBot.EF.Entities;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public interface IAskConfirmationService
    {
        Task SendConfirmationAsync(Message message, UserSession session);
    }
}

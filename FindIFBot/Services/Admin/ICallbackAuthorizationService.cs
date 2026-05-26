using Telegram.Bot.Types;

namespace FindIFBot.Services.Admin
{
    public interface ICallbackAuthorizationService
    {
        Task<bool> IsAuthorizedAsync(CallbackQuery callback, AdminCallbackData data);
    }
}

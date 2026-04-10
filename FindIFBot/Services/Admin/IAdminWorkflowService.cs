using FindIFBot.Domain;
using FindIFBot.Persistence;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Admin
{
    public interface IAdminWorkflowService
    {
        Task HandleCallbackAsync(CallbackQuery callback);
        Task SubmitAskAsync(StoredMessage stored, UserInfo userInfo);
    }
}

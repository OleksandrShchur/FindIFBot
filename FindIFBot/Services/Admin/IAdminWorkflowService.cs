using FindIFBot.Persistence;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Admin
{
    public interface IAdminWorkflowService
    {
        Task HandleCallbackAsync(CallbackQuery callback);
        Task SubmitFindAsync(StoredMessage stored);
        Task SubmitAdAsync(Message message);
    }
}

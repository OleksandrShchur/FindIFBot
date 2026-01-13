using Telegram.Bot.Types;

namespace FindIFBot.Services.Admin
{
    public interface IAdminWorkflowService
    {
        Task HandleCallbackAsync(CallbackQuery callback);
        Task SubmitFindAsync(Message message);
        Task SubmitAdAsync(Message message);
    }
}

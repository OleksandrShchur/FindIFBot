using FindIFBot.Domain;
using FindIFBot.Persistence;

namespace FindIFBot.Services.Admin
{
    public interface IAdminRequestNotifier
    {
        Task SendToAdminAsync(StoredMessage stored, UserInfo userInfo);
    }
}

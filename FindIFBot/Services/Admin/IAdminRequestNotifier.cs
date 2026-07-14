using FindIFBot.Domain;
using FindIFBot.Persistence;

namespace FindIFBot.Services.Admin
{
    public interface IAdminRequestNotifier
    {
        /// <summary>
        /// Sends the moderation thread to the admin and returns the message id of the
        /// first (user-info) message, which is never deleted and can be used as a reply anchor.
        /// </summary>
        Task<int> SendToAdminAsync(StoredMessage stored, UserInfo userInfo);
    }
}

using FindIFBot.Persistence;

namespace FindIFBot.Services.Admin
{
    public interface IRequestPublisher
    {
        Task<string> PublishAsync(StoredMessage stored);
    }
}

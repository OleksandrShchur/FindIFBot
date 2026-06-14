namespace FindIFBot.Persistence
{
    public interface IMessageStore
    {
        Task StoreAsync(StoredMessage message, CancellationToken cancellationToken = default);
        Task<StoredMessage?> TryGetAsync(int messageId, CancellationToken cancellationToken = default);
        Task RemoveAsync(int messageId, CancellationToken cancellationToken = default);
    }
}

namespace FindIFBot.Services.Ask
{
    public interface ISubscriptionService
    {
        Task<bool> IsSubscribedToOutputChannelAsync(long userId);
    }
}

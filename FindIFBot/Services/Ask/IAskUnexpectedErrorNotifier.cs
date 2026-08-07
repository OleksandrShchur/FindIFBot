namespace FindIFBot.Services.Ask
{
    public interface IAskUnexpectedErrorNotifier
    {
        /// <summary>
        /// Resets the user session to Idle and sends a Ukrainian unexpected-error message
        /// with a Direct Chat button. Best-effort: never throws.
        /// </summary>
        Task NotifyAsync(long chatId, long userId);
    }
}

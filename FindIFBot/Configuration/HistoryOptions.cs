namespace FindIFBot.Configuration
{
    /// <summary>
    /// Configurable limits for rendering a user's request history. Bound from the "History"
    /// configuration section. Caps how many items are listed per section and how many
    /// per-request reply messages are sent, preventing a flood of Telegram calls (and possible
    /// rate-limit/timeout) for users with very large histories.
    /// </summary>
    public sealed class HistoryOptions
    {
        public const string SectionName = "History";

        public int MaxItemsPerSection { get; init; } = 10;
    }
}

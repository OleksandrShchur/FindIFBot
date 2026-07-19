using System.Linq;

namespace FindIFBot.Helpers
{
    /// <summary>
    /// Single source of truth for user-facing command triggers: slash commands and the
    /// localized reply-keyboard button captions that map to them. Previously these strings
    /// were duplicated as magic literals across the dispatcher, message routers and flow
    /// services, which made it easy for the keyboard captions and the matching logic to
    /// drift out of sync. All comparisons are performed against the normalized
    /// (trimmed + lower-cased) form produced by <see cref="Normalize"/>.
    /// </summary>
    public static class BotCommands
    {
        public const string Start = "/start";
        public const string Ask = "/ask";
        public const string Ads = "/ads";
        public const string Help = "/help";
        public const string Policy = "/policy";
        public const string Support = "/support";
        public const string Channel = "/channel";
        public const string History = "/history";
        public const string Pending = "/pending";
        public const string MainMenuCallback = "main_menu";

        // Trigger sets are stored in normalized form so they can be compared directly
        // against the output of Normalize(). Keep the button captions in sync with Keyboards.cs.
        private static readonly string[] AskTriggers = { Ask, "📨 надіслати запит", "надіслати запит", "запит" };
        private static readonly string[] HelpTriggers = { Help, "ℹ️ довідка", "довідка", "допомога" };
        private static readonly string[] PolicyTriggers = { Policy, "📜 правила", "правила" };
        private static readonly string[] SupportTriggers = { Support, "❤️ підтримати", "підтримати" };
        private static readonly string[] ChannelTriggers = { Channel, "🔗 канал", "канал" };
        private static readonly string[] HistoryTriggers = { History, "📋 історія запитів", "історія запитів", "історія" };
        private static readonly string[] AdsCollabTriggers = { Ads, "🤝 реклама та співпраця", "реклама та співпраця", "реклама", "співпраця" };
        private static readonly string[] PendingTriggers =
        {
            Pending,
            "⏳ черга модерації",
            "черга модерації"
        };

        /// <summary>Trims and lower-cases input so command matching is case-insensitive and whitespace-tolerant.</summary>
        public static string Normalize(string? value) =>
            value?.Trim().ToLowerInvariant() ?? string.Empty;

        public static bool IsStart(string normalized) => normalized == Start;
        public static bool IsAsk(string normalized) => AskTriggers.Contains(normalized);
        public static bool IsHelp(string normalized) => HelpTriggers.Contains(normalized);
        public static bool IsPolicy(string normalized) => PolicyTriggers.Contains(normalized);
        public static bool IsSupport(string normalized) => SupportTriggers.Contains(normalized);
        public static bool IsChannel(string normalized) => ChannelTriggers.Contains(normalized);
        public static bool IsHistory(string normalized) => HistoryTriggers.Contains(normalized);
        public static bool IsAdsCollaboration(string normalized) => AdsCollabTriggers.Contains(normalized);
        public static bool IsPending(string normalized) => PendingTriggers.Contains(normalized);
        public static bool IsMainMenu(string normalized) => normalized == MainMenuCallback;
    }
}

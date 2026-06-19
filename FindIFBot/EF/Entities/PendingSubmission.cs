namespace FindIFBot.EF.Entities
{
    /// <summary>
    /// Durable copy of a submission that is awaiting user confirmation or admin moderation.
    /// Previously this content lived only in an in-memory dictionary, so a process restart
    /// between submission and moderation made the content unrecoverable (admin approve/reject
    /// failed with "stored message not found"). Persisting it here removes that fragility.
    /// Keyed by the user's Telegram message id to mirror the original lookup semantics.
    /// </summary>
    public class PendingSubmission
    {
        public int MessageId { get; init; }
        public long ChatId { get; init; }
        public long UserId { get; init; }
        public string? Text { get; set; }

        /// <summary>JSON-serialized list of Telegram photo FileIds.</summary>
        public string PhotosJson { get; set; } = "[]";

        public string? MediaGroupId { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}

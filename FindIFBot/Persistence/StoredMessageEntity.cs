namespace FindIFBot.Persistence
{
    /// <summary>
    /// Serializable subset of Telegram <c>MessageEntity</c> needed to reconstruct formatting
    /// (especially <c>text_link</c>) when re-sending a submission.
    /// Offsets/lengths use UTF-16 code units, matching Telegram's encoding.
    /// </summary>
    public record StoredMessageEntity(
        string Type,
        int Offset,
        int Length,
        string? Url = null,
        string? Language = null);
}

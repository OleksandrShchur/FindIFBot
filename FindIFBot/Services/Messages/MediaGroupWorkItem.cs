namespace FindIFBot.Services.Messages
{
    /// <summary>
    /// Identifies a buffered media group that is ready to be drained and processed.
    /// Enqueued when the first message of an album arrives; the background processor
    /// applies the buffering delay before taking the accumulated messages.
    /// </summary>
    public readonly record struct MediaGroupWorkItem(long UserId, string MediaGroupId);
}

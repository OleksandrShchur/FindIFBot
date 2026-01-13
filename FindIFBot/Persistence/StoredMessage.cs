namespace FindIFBot.Persistence
{
    public record StoredMessage(
        long ChatId,
        long UserId,
        string? Text,
        bool HasPhoto
    );
}

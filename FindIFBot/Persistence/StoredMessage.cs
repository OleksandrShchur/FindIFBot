namespace FindIFBot.Persistence
{
    public record StoredMessage(
        long ChatId,
        long UserId,
        string? Text,                 // text or caption
        IReadOnlyList<string> Photos, // FileIds, empty if none
        string? MediaGroupId          // null if single message
    )
    {
        public bool HasPhotos => Photos.Count > 0;
        public bool IsAlbum => MediaGroupId != null;
    }
}

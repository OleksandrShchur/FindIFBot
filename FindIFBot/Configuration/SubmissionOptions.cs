namespace FindIFBot.Configuration
{
    /// <summary>
    /// Configurable submission limits. Bound from the "Submission" configuration section.
    /// Defaults preserve the original hard-coded values so behavior is unchanged when the
    /// section is absent. Limits are derived from Telegram's caption/message length ceilings,
    /// leaving headroom for the appended post template.
    /// </summary>
    public sealed class SubmissionOptions
    {
        public const string SectionName = "Submission";

        public int MaxCaptionLength { get; init; } = 970;
        public int MaxTextLength { get; init; } = 4040;
        public int MaxAlbumPhotoCount { get; init; } = 10;
    }
}

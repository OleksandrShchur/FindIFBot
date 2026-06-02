namespace FindIFBot.Services.Messages
{
    public sealed record SubmissionValidationResult(bool IsValid, string? ErrorMessage)
    {
        public static SubmissionValidationResult Valid() => new(true, null);
        public static SubmissionValidationResult Invalid(string errorMessage) => new(false, errorMessage);
    }
}

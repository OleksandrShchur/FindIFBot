using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public interface ISubmissionValidator
    {
        SubmissionValidationResult ValidateSingleMessage(Message message, string? text, int photoCount);
        SubmissionValidationResult ValidateMediaGroup(IReadOnlyList<Message> messages, int photoCount, int ignoredCount, string caption);
    }
}

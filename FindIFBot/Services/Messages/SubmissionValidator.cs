using FindIFBot.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace FindIFBot.Services.Messages
{
    public class SubmissionValidator : ISubmissionValidator
    {
        private readonly SubmissionOptions _limits;

        public SubmissionValidator(IOptions<SubmissionOptions> limits)
        {
            _limits = limits.Value;
        }

        public SubmissionValidationResult ValidateSingleMessage(Message message, string? text, int photoCount)
        {
            var hasNonPhotoMedia = message.Video != null ||
                                   message.Animation != null ||
                                   message.Document != null ||
                                   message.Audio != null ||
                                   message.Voice != null ||
                                   message.Sticker != null;

            if (hasNonPhotoMedia)
            {
                return SubmissionValidationResult.Invalid(
                    "❌ <b>Помилка:</b> надіслано не фото\n\n" +
                    "Ми підтримуємо <b>тільки фотографії</b>.\n" +
                    "Відео, документи, GIF, стікери та інші типи файлів зараз не обробляються.");
            }

            var textToValidate = text ?? string.Empty;
            var limit = photoCount > 0 ? _limits.MaxCaptionLength : _limits.MaxTextLength;

            if (textToValidate.Length > limit)
            {
                return SubmissionValidationResult.Invalid(
                    $"❌ <b>Помилка:</b> текст занадто довгий\n\n" +
                    $"<b>Максимум дозволено:</b> {limit} символів\n" +
                    $"<b>Ваш текст:</b> {textToValidate.Length} символів\n\n" +
                    "Будь ласка, скоротіть текст і спробуйте ще раз.");
            }

            return SubmissionValidationResult.Valid();
        }

        public SubmissionValidationResult ValidateMediaGroup(
            IReadOnlyList<Message> messages,
            int photoCount,
            int ignoredCount,
            string caption)
        {
            if (photoCount > _limits.MaxAlbumPhotoCount)
            {
                return SubmissionValidationResult.Invalid(
                    "❌ <b>Помилка:</b> забагато фотографій\n" +
                    $"<b>Максимум дозволено:</b> {_limits.MaxAlbumPhotoCount} фото в одному запиті\n\n" +
                    "Будь ласка, надішліть менше.");
            }

            if (photoCount == 0)
            {
                return SubmissionValidationResult.Invalid(
                    "❌ <b>Помилка:</b> в альбомі немає фотографій\n\n" +
                    "Надішліть, будь ласка, альбом саме з фото.");
            }

            if (caption.Length > _limits.MaxCaptionLength)
            {
                return SubmissionValidationResult.Invalid(
                    $"❌ <b>Помилка:</b> підпис до фото занадто довгий\n\n" +
                    $"<b>Максимум дозволено:</b> {_limits.MaxCaptionLength} символів\n" +
                    $"<b>Ваш підпис:</b> {caption.Length} символів\n\n" +
                    "Будь ласка, скоротіть підпис і спробуйте ще раз.");
            }

            return SubmissionValidationResult.Valid();
        }
    }
}

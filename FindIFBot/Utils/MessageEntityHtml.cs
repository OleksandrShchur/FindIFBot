using System.Net;
using System.Text;
using FindIFBot.Persistence;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FindIFBot.Utils
{
    /// <summary>
    /// Converts Telegram message text + entities into HTML for ParseMode.Html re-sends.
    /// </summary>
    public static class MessageEntityHtml
    {
        public static IReadOnlyList<StoredMessageEntity> FromTelegram(IEnumerable<MessageEntity>? entities)
        {
            if (entities is null)
            {
                return Array.Empty<StoredMessageEntity>();
            }

            return entities
                .Select(e => new StoredMessageEntity(
                    Type: e.Type.ToString(),
                    Offset: e.Offset,
                    Length: e.Length,
                    Url: e.Url,
                    Language: e.Language))
                .ToList();
        }

        public static string Format(string? text, IReadOnlyList<StoredMessageEntity>? entities)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (entities is null || entities.Count == 0)
            {
                return Escape(text);
            }

            var sorted = entities
                .Where(e => e.Offset >= 0 && e.Length > 0 && e.Offset + e.Length <= text.Length)
                .OrderBy(e => e.Offset)
                .ThenByDescending(e => e.Length)
                .ToList();

            return Encode(text, sorted, 0, text.Length);
        }

        private static string Encode(
            string text,
            IReadOnlyList<StoredMessageEntity> entities,
            int start,
            int end)
        {
            var sb = new StringBuilder();
            var i = 0;

            while (start < end)
            {
                while (i < entities.Count && entities[i].Offset + entities[i].Length <= start)
                {
                    i++;
                }

                if (i >= entities.Count || entities[i].Offset >= end)
                {
                    sb.Append(Escape(text[start..end]));
                    break;
                }

                var entity = entities[i];
                if (entity.Offset > start)
                {
                    sb.Append(Escape(text[start..entity.Offset]));
                    start = entity.Offset;
                    continue;
                }

                var entityEnd = Math.Min(entity.Offset + entity.Length, end);
                var nested = entities
                    .Skip(i + 1)
                    .Where(e => e.Offset >= entity.Offset && e.Offset + e.Length <= entityEnd)
                    .ToList();

                var inner = Encode(text, nested, entity.Offset, entityEnd);
                sb.Append(Wrap(entity, inner));
                start = entityEnd;
                i++;
            }

            return sb.ToString();
        }

        private static string Wrap(StoredMessageEntity entity, string inner)
        {
            if (!Enum.TryParse<MessageEntityType>(entity.Type, ignoreCase: true, out var type))
            {
                return inner;
            }

            return type switch
            {
                MessageEntityType.Bold => $"<b>{inner}</b>",
                MessageEntityType.Italic => $"<i>{inner}</i>",
                MessageEntityType.Underline => $"<u>{inner}</u>",
                MessageEntityType.Strikethrough => $"<s>{inner}</s>",
                MessageEntityType.Spoiler => $"<tg-spoiler>{inner}</tg-spoiler>",
                MessageEntityType.Code => $"<code>{inner}</code>",
                MessageEntityType.Pre => string.IsNullOrEmpty(entity.Language)
                    ? $"<pre>{inner}</pre>"
                    : $"<pre><code class=\"language-{EscapeAttribute(entity.Language)}\">{inner}</code></pre>",
                MessageEntityType.TextLink when !string.IsNullOrEmpty(entity.Url) =>
                    $"<a href=\"{EscapeAttribute(entity.Url)}\">{inner}</a>",
                MessageEntityType.Url => $"<a href=\"{EscapeAttribute(WebUtility.HtmlDecode(inner))}\">{inner}</a>",
                MessageEntityType.Email => $"<a href=\"mailto:{EscapeAttribute(WebUtility.HtmlDecode(inner))}\">{inner}</a>",
                MessageEntityType.Blockquote or MessageEntityType.ExpandableBlockquote =>
                    $"<blockquote>{inner}</blockquote>",
                _ => inner
            };
        }

        private static string Escape(string value) =>
            value
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);

        private static string EscapeAttribute(string value) =>
            Escape(value).Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}

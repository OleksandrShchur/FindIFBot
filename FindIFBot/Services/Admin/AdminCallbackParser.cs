namespace FindIFBot.Services.Admin
{
    public class AdminCallbackParser : IAdminCallbackParser
    {
        public bool TryParse(string? callbackData, out AdminCallbackData data)
        {
            data = new AdminCallbackData(string.Empty, 0, 0);

            var parts = callbackData?.Split('|');
            if (parts == null || parts.Length < 3)
            {
                return false;
            }

            if (!long.TryParse(parts[1], out var userId) ||
                !int.TryParse(parts[2], out var messageId))
            {
                return false;
            }

            data = new AdminCallbackData(parts[0], userId, messageId);
            return true;
        }
    }
}

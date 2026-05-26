namespace FindIFBot.Services.Admin
{
    public sealed record AdminCallbackData(string Action, long UserId, int MessageId)
    {
        public bool IsUserAction => Action is "proceed" or "cancel";
    }
}

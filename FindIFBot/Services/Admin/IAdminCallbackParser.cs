namespace FindIFBot.Services.Admin
{
    public interface IAdminCallbackParser
    {
        bool TryParse(string? callbackData, out AdminCallbackData data);
    }
}

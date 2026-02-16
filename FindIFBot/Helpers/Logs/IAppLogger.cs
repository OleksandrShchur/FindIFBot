namespace FindIFBot.Helpers.Logs
{
    public interface IAppLogger<T>
    {
        Task LogInfo(string component, string message);
        Task LogWarning(string component, string message);
        Task LogError(string component, string message);
    }
}

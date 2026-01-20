namespace FindIFBot.Helpers.Logs
{
    public interface IAppLogger
    {
        void Log(string component, LogType level, string message);
    }
}

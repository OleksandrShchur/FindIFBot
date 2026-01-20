namespace FindIFBot.Helpers.Logs
{
    public class AppLogger : IAppLogger
    {
        private readonly object _logLock = new();
        private const string LogPath = "requests.log";

        public void Log(string component, LogType level, string message)
        {
            var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{component}] - [{level.ToString().ToUpper()}]: {message}";
            lock (_logLock)
            {
                try
                {
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
                catch { }
            }
        }
    }
}

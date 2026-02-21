namespace FindIFBot.Services
{
    public interface IMaintenanceService
    {
        Task ProcessYesterdayLogsAsync(CancellationToken cancellationToken = default);
    }
}

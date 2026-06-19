using FindIFBot.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FindIFBot.IntegrationTests.Http
{
    /// <summary>
    /// Boots the real application via <see cref="WebApplicationFactory{TEntryPoint}"/> but
    /// replaces the two external-facing seams the HTTP tests care about — the command
    /// dispatcher (Telegram side effects) and the maintenance service (channel/log side
    /// effects) — with NSubstitute doubles. Everything else (routing, model binding,
    /// rate limiting, auth header parsing) runs for real.
    /// </summary>
    public class TestWebAppFactory : WebApplicationFactory<Program>
    {
        public const string MaintenanceKey = "integration-secret-key";

        public ICommandDispatcher Dispatcher { get; } = Substitute.For<ICommandDispatcher>();
        public IMaintenanceService Maintenance { get; } = Substitute.For<IMaintenanceService>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telegram:BotToken"] = "123456:test-token",
                    ["Maintenance:SecretKey"] = MaintenanceKey,
                    // A syntactically valid connection string; the DB is never hit because the
                    // dispatcher/maintenance seams are substituted in these HTTP tests.
                    ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=FindIFBotTests;Trusted_Connection=True;"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICommandDispatcher>();
                services.AddScoped(_ => Dispatcher);

                services.RemoveAll<IMaintenanceService>();
                services.AddScoped(_ => Maintenance);
            });
        }
    }
}

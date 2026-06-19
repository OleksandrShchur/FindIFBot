using FindIFBot.EF;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FindIFBot.IntegrationTests.Repositories
{
    /// <summary>
    /// A per-test relational database backed by SQLite in-memory. The connection is kept open
    /// for the lifetime of the instance (closing it would drop the schema and data). SQLite is
    /// used instead of the EF InMemory provider because the repositories rely on relational
    /// features such as <c>ExecuteUpdateAsync</c>.
    /// </summary>
    public sealed class SqliteTestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<BotDbContext> _options;

        public SqliteTestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<BotDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        /// <summary>Creates a fresh context (clean change tracker) over the shared connection.</summary>
        public BotDbContext CreateContext() => new(_options);

        public void Dispose() => _connection.Dispose();
    }
}

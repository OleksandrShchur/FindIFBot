using FindIFBot.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FindIFBot.EF
{
    public class BotDbContext : DbContext
    {
        public DbSet<UserSession> UserSessions { get; set; }

        public BotDbContext(DbContextOptions<BotDbContext> options)
        : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserSession>(e =>
            {
                e.HasKey(s => s.UserId);
                e.Property(s => s.UserId).ValueGeneratedNever();
                e.Property(s => s.State).HasConversion<int>();
            });
        }
    }
}

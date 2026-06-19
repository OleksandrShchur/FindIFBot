using FindIFBot.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FindIFBot.EF
{
    public class BotDbContext : DbContext
    {
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserRequest> UserRequests { get; set; }
        public DbSet<PendingSubmission> PendingSubmissions { get; set; }

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

            modelBuilder.Entity<UserRequest>(e =>
            {
                e.HasKey(e => e.Id);

                e.Property(e => e.Id)
                    .ValueGeneratedNever();

                e.Property(e => e.UserId)
                    .IsRequired();

                e.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<int>();

                e.Property(e => e.ChannelLink)
                    .HasMaxLength(500);

                e.Property(e => e.SubmittedAt)
                    .IsRequired();

                e.Property(e => e.UserMessageId)
                    .IsRequired();

                e.HasIndex(e => e.UserId);
                e.HasIndex(e => new { e.UserId, e.SubmittedAt });
                e.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<PendingSubmission>(e =>
            {
                e.HasKey(p => p.MessageId);
                e.Property(p => p.MessageId).ValueGeneratedNever();
                e.Property(p => p.UserId).IsRequired();
                e.Property(p => p.ChatId).IsRequired();
                e.Property(p => p.PhotosJson).IsRequired();
                e.Property(p => p.MediaGroupId).HasMaxLength(64);
                e.Property(p => p.CreatedAtUtc).IsRequired();

                e.HasIndex(p => p.CreatedAtUtc);
            });
        }
    }
}

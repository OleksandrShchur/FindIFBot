using System.Text.Json;
using FindIFBot.EF;
using FindIFBot.EF.Entities;
using Microsoft.EntityFrameworkCore;

namespace FindIFBot.Persistence
{
    /// <summary>
    /// Database-backed <see cref="IMessageStore"/>. Pending submission content survives process
    /// restarts so admin moderation can still publish/reject a request after a redeploy.
    /// Stale rows are pruned on write to bound table growth (parity with the previous in-memory TTL).
    /// </summary>
    public class DbMessageStore : IMessageStore
    {
        private static readonly TimeSpan EntryLifetime = TimeSpan.FromHours(48);

        private readonly BotDbContext _db;

        public DbMessageStore(BotDbContext db)
        {
            _db = db;
        }

        public async Task StoreAsync(StoredMessage message, CancellationToken cancellationToken = default)
        {
            await PruneExpiredAsync(cancellationToken);

            var entity = await _db.PendingSubmissions
                .FirstOrDefaultAsync(p => p.MessageId == message.MessageId, cancellationToken);

            var photosJson = JsonSerializer.Serialize(message.Photos);
            var entitiesJson = message.TextEntities.Count > 0
                ? JsonSerializer.Serialize(message.TextEntities)
                : null;

            if (entity is null)
            {
                _db.PendingSubmissions.Add(new PendingSubmission
                {
                    MessageId = message.MessageId,
                    ChatId = message.ChatId,
                    UserId = message.UserId,
                    Text = message.Text,
                    PhotosJson = photosJson,
                    EntitiesJson = entitiesJson,
                    MediaGroupId = message.MediaGroupId,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                entity.Text = message.Text;
                entity.PhotosJson = photosJson;
                entity.EntitiesJson = entitiesJson;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<StoredMessage?> TryGetAsync(int messageId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.PendingSubmissions
                .FirstOrDefaultAsync(p => p.MessageId == messageId, cancellationToken);

            if (entity is null)
            {
                return null;
            }

            if (DateTime.UtcNow - entity.CreatedAtUtc > EntryLifetime)
            {
                _db.PendingSubmissions.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);
                return null;
            }

            var photos = JsonSerializer.Deserialize<List<string>>(entity.PhotosJson) ?? new List<string>();
            var entities = string.IsNullOrEmpty(entity.EntitiesJson)
                ? null
                : JsonSerializer.Deserialize<List<StoredMessageEntity>>(entity.EntitiesJson);

            return new StoredMessage(
                entity.ChatId,
                entity.UserId,
                entity.Text,
                photos,
                entity.MediaGroupId,
                entity.MessageId,
                entities);
        }

        public async Task RemoveAsync(int messageId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.PendingSubmissions
                .FirstOrDefaultAsync(p => p.MessageId == messageId, cancellationToken);

            if (entity is not null)
            {
                _db.PendingSubmissions.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task PruneExpiredAsync(CancellationToken cancellationToken)
        {
            var cutoff = DateTime.UtcNow - EntryLifetime;
            var stale = await _db.PendingSubmissions
                .Where(p => p.CreatedAtUtc < cutoff)
                .ToListAsync(cancellationToken);

            if (stale.Count > 0)
            {
                _db.PendingSubmissions.RemoveRange(stale);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

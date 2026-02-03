using Microsoft.EntityFrameworkCore;
using Ingestion.Core.Domain;

namespace Ingestion.Core.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<IngestionEvent> IngestionEvents => Set<IngestionEvent>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    }
}

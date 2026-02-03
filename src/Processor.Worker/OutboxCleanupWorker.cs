using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ingestion.Core.Infrastructure.Persistence;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Processor.Worker
{
    public class OutboxCleanupWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public OutboxCleanupWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var toDelete = db.OutboxMessages.Where(o => o.Published == true).ToList();

                if (toDelete.Any())
                {
                    db.OutboxMessages.RemoveRange(toDelete);
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[OutboxCleanup] Eliminados {toDelete.Count} mensajes publicados");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // cada minuto
            }
        }
    }
}

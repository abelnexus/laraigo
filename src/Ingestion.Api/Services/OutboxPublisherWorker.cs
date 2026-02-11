using Ingestion.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;

public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConnectionFactory _factory;

    public OutboxPublisherWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Hostname debe coincidir con tu docker-compose
        _factory = new ConnectionFactory() { HostName = "rabbitmq" }; 
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. Obtener mensajes pendientes de publicación
                var pendingMessages = await dbContext.OutboxMessages
                    .Where(m => !m.Published)
                    .OrderBy(m => m.CreatedAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (pendingMessages.Any())
                {
                    try
                    {
                        using var connection = _factory.CreateConnection();
                        using var channel = connection.CreateModel();

                        channel.QueueDeclare(queue: "ingestionQueue", durable: true, exclusive: false, autoDelete: false);

                        foreach (var message in pendingMessages)
                        {
                            var body = Encoding.UTF8.GetBytes(message.Payload);
                            
                            channel.BasicPublish(exchange: "", routingKey: "ingestionQueue", basicProperties: null, body: body);
                            
                            message.Published = true; // 2. Marcar como enviado
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                        Console.WriteLine($"✅ [Outbox] {pendingMessages.Count} mensajes enviados a RabbitMQ.");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("⚠️ [Outbox] RabbitMQ no disponible. Reintentando en el próximo ciclo...");
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
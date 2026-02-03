using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ingestion.Core.Infrastructure.Persistence;

namespace Processor.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public Worker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory() { HostName = "rabbitmq" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: "ingestionQueue",
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.WriteLine($"Procesando mensaje: {message}");

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try
                {
                    var ingestionEvent = db.IngestionEvents.FirstOrDefault(e => e.Status == "Pending");
                    if (ingestionEvent != null)
                    {
                        ingestionEvent.Status = "Processed";

                        var outbox = db.OutboxMessages.FirstOrDefault(o => o.Payload == ingestionEvent.Payload);
                        if (outbox != null)
                        {
                            outbox.Published = true;
                        }

                        db.SaveChanges();
                    }

                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error procesando mensaje: {ex.Message}");
                    // opcional: no ack, para reintento
                }
            };

            channel.BasicConsume(queue: "ingestionQueue", autoAck: false, consumer: consumer);

            return Task.Delay(Timeout.Infinite, stoppingToken); // Mantener worker vivo

        }
    }
}

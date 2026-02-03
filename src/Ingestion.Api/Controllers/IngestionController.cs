using Microsoft.AspNetCore.Mvc;
using Ingestion.Core.Infrastructure.Persistence;
using Ingestion.Core.Domain;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Ingestion.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IngestionController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IConnection _rabbitConnection;

        // 1️⃣ Inyectamos IConnection (que ya configuramos en Program.cs)
        public IngestionController(AppDbContext dbContext, IConnection rabbitConnection)
        {
            _dbContext = dbContext;
            _rabbitConnection = rabbitConnection;
        }

        [HttpPost("ingest")]
        public IActionResult Ingest([FromBody] IngestionEventRequest request)
        {
            var ingestionEvent = new IngestionEvent
            {
                Id = Guid.NewGuid(),
                Payload = request.Payload,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.IngestionEvents.Add(ingestionEvent);

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = ingestionEvent.Id,
                Type = "IngestionEventCreated",
                Payload = JsonSerializer.Serialize(ingestionEvent),
                Published = false,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.OutboxMessages.Add(outboxMessage);
            _dbContext.SaveChanges();

            // 2️⃣ Usamos la conexión inyectada para crear un canal temporal
            try
            {
                using var channel = _rabbitConnection.CreateModel();
                
                // Declarar la cola (por si no existe)
                channel.QueueDeclare(
                    queue: "ingestionQueue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ingestionEvent));
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(
                    exchange: "",
                    routingKey: "ingestionQueue",
                    basicProperties: properties,
                    body: body
                );

                outboxMessage.Published = true;
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                // Si falla Rabbit, el OutboxWorker lo procesará después.
                Console.WriteLine($"Error al publicar en RabbitMQ: {ex.Message}");
            }

            return Ok(new { ingestionEvent.Id });
        }
    }
}
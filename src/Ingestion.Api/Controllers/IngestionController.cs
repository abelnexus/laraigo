using Microsoft.AspNetCore.Mvc;
using Ingestion.Core.Infrastructure.Persistence;
using Ingestion.Core.Domain;
using System.Text.Json;

namespace Ingestion.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IngestionController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        // Ahora solo dependemos del DbContext
        public IngestionController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> Ingest([FromBody] IngestionEventRequest request)
        {
            // Iniciamos transacción para asegurar que IngestionEvent y OutboxMessage se guarden juntos
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // 1. Crear el evento principal
                var ingestionEvent = new IngestionEvent
                {
                    Id = Guid.NewGuid(),
                    Payload = request.Payload,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.IngestionEvents.Add(ingestionEvent);

                // 2. Crear el mensaje en la tabla Outbox
                // Este es el que el OutboxPublisherWorker leerá después
                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    AggregateId = ingestionEvent.Id,
                    Type = "IngestionEventCreated",
                    Payload = JsonSerializer.Serialize(ingestionEvent),
                    Published = false, // Siempre false, el Worker lo cambiará a true
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.OutboxMessages.Add(outboxMessage);

                // 3. Persistir cambios y confirmar transacción
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // 4. Responder al cliente inmediatamente
                return Ok(new { ingestionEvent.Id });
            }
            catch (Exception ex)
            {
                // Si algo falla aquí, la DB hace rollback y no se pierde la integridad
                await transaction.RollbackAsync();
                Console.WriteLine($"Error en Controller: {ex.Message}");
                return StatusCode(500, "Error al procesar la solicitud en la base de datos.");
            }
        }
    }
}
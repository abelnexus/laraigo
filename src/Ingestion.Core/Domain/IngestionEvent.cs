using System;

namespace Ingestion.Core.Domain
{
    public class IngestionEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Payload { get; init; } = default!;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
}

public class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AggregateId { get; init; }  // << agrega esto
    public string Type { get; init; } = default!;
    public string Payload { get; init; } = default!;
    public bool Published { get; set; } = false;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

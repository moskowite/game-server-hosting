namespace GameServerHosting.Domain.Entities;

/// <summary>
/// Represents an outbox message for transactional event publishing
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier for the outbox message
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of the event (full type name)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized event payload (JSON)
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// When the event was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the event was processed (null if not yet processed)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Number of processing attempts
    /// </summary>
    public int ProcessingAttempts { get; set; } = 0;

    /// <summary>
    /// Correlation ID for tracking related events
    /// </summary>
    public Guid? CorrelationId { get; set; }
}

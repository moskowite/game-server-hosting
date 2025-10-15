namespace GameServerHosting.Shared.Events;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class BaseEvent
{
    /// <summary>
    /// Unique identifier for this event
    /// </summary>
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Correlation ID to track the entire operation across multiple events
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Causation ID - the ID of the event that caused this event
    /// </summary>
    public Guid? CausationId { get; set; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID associated with this event (if applicable)
    /// </summary>
    public Guid? UserId { get; set; }
}

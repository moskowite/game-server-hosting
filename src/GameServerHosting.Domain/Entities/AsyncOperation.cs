using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Domain.Entities;

/// <summary>
/// Represents a long-running asynchronous operation that can be tracked by users
/// </summary>
public class AsyncOperation
{
    /// <summary>
    /// Unique identifier for the operation
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who initiated the operation
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of operation (e.g., "ServerProvisioning", "PaymentCapture")
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the operation
    /// </summary>
    public AsyncOperationStatus Status { get; set; } = AsyncOperationStatus.Pending;

    /// <summary>
    /// Associated resource ID (e.g., GameServerId, OrderId)
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Resource type (e.g., "GameServer", "Order")
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Correlation ID linking to domain events
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Current progress percentage (0-100)
    /// </summary>
    public int? ProgressPercentage { get; set; }

    /// <summary>
    /// Human-readable status message
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if operation failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// When the operation was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the operation was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the operation completed (success or failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// User navigation property
    /// </summary>
    public virtual ApplicationUser? User { get; set; }
}

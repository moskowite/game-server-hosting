namespace GameServerHosting.Domain.Entities;

/// <summary>
/// Stores idempotency keys to prevent duplicate request processing
/// </summary>
public class IdempotencyKey
{
    /// <summary>
    /// The idempotency key provided by the client
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// User ID associated with this key
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of request (e.g., "CreateOrder", "ProvisionServer")
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Associated resource ID (e.g., OrderId, GameServerId)
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// HTTP status code of the original response
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Serialized response body (JSON)
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// When the key was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the key expires (null for no expiration)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

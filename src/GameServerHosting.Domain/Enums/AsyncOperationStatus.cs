namespace GameServerHosting.Domain.Enums;

/// <summary>
/// Status of an asynchronous operation
/// </summary>
public enum AsyncOperationStatus
{
    /// <summary>
    /// Operation is pending and has not started
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Operation is currently in progress
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Operation completed successfully
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Operation failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Operation was cancelled
    /// </summary>
    Cancelled = 4
}

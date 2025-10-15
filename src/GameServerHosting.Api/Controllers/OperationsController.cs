using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// Controller for tracking asynchronous operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OperationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OperationsController> _logger;

    public OperationsController(IUnitOfWork unitOfWork, ILogger<OperationsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Gets the status of an asynchronous operation by ID
    /// </summary>
    /// <param name="id">The operation ID</param>
    /// <returns>The operation status</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AsyncOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AsyncOperationResponse>> GetOperation(Guid id)
    {
        var userId = GetUserId();
        
        var operation = await _unitOfWork.AsyncOperations.GetByIdAsync(id);
        
        if (operation == null)
        {
            return NotFound(new { message = "Operation not found" });
        }

        // Ensure user can only access their own operations
        if (operation.UserId != userId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access operation {OperationId} belonging to user {OperationUserId}",
                userId, id, operation.UserId);
            return Forbid();
        }

        return Ok(new AsyncOperationResponse
        {
            Id = operation.Id,
            OperationType = operation.OperationType,
            Status = operation.Status.ToString(),
            ResourceId = operation.ResourceId,
            ResourceType = operation.ResourceType,
            ProgressPercentage = operation.ProgressPercentage,
            StatusMessage = operation.StatusMessage,
            ErrorMessage = operation.ErrorMessage,
            ErrorCode = operation.ErrorCode,
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt,
            CompletedAt = operation.CompletedAt
        });
    }

    /// <summary>
    /// Gets the status of an operation by correlation ID
    /// </summary>
    /// <param name="correlationId">The correlation ID</param>
    /// <returns>The operation status</returns>
    [HttpGet("by-correlation/{correlationId}")]
    [ProducesResponseType(typeof(AsyncOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AsyncOperationResponse>> GetOperationByCorrelation(Guid correlationId)
    {
        var userId = GetUserId();
        
        var operation = await _unitOfWork.AsyncOperations.GetByCorrelationIdAsync(correlationId);
        
        if (operation == null)
        {
            return NotFound(new { message = "Operation not found" });
        }

        // Ensure user can only access their own operations
        if (operation.UserId != userId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access operation with correlation {CorrelationId} belonging to user {OperationUserId}",
                userId, correlationId, operation.UserId);
            return Forbid();
        }

        return Ok(new AsyncOperationResponse
        {
            Id = operation.Id,
            OperationType = operation.OperationType,
            Status = operation.Status.ToString(),
            ResourceId = operation.ResourceId,
            ResourceType = operation.ResourceType,
            ProgressPercentage = operation.ProgressPercentage,
            StatusMessage = operation.StatusMessage,
            ErrorMessage = operation.ErrorMessage,
            ErrorCode = operation.ErrorCode,
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt,
            CompletedAt = operation.CompletedAt
        });
    }

    /// <summary>
    /// Gets all operations for the current user
    /// </summary>
    /// <returns>List of operations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AsyncOperationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<AsyncOperationResponse>>> GetUserOperations()
    {
        var userId = GetUserId();
        
        var operations = await _unitOfWork.AsyncOperations.GetByUserIdAsync(userId);
        
        var response = operations.Select(o => new AsyncOperationResponse
        {
            Id = o.Id,
            OperationType = o.OperationType,
            Status = o.Status.ToString(),
            ResourceId = o.ResourceId,
            ResourceType = o.ResourceType,
            ProgressPercentage = o.ProgressPercentage,
            StatusMessage = o.StatusMessage,
            ErrorMessage = o.ErrorMessage,
            ErrorCode = o.ErrorCode,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            CompletedAt = o.CompletedAt
        }).ToList();

        return Ok(response);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

#region Response Models

/// <summary>
/// Response model for async operation status
/// </summary>
public class AsyncOperationResponse
{
    /// <summary>
    /// Operation ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of operation
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Associated resource ID
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Resource type
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int? ProgressPercentage { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// When the operation was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the operation was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the operation completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

#endregion

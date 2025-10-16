using GameServerHosting.Application.DTOs;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using GameServerHosting.Shared.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// Asynchronous order controller demonstrating event-driven architecture
/// </summary>
[ApiController]
[Route("api/orders-async")]
[Authorize]
internal class AsyncOrdersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<AsyncOrdersController> _logger;

    public AsyncOrdersController(
        IUnitOfWork unitOfWork,
        IOutboxService outboxService,
        ILogger<AsyncOrdersController> logger)
    {
        _unitOfWork = unitOfWork;
        _outboxService = outboxService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new server order asynchronously using events
    /// </summary>
    /// <param name="request">Order creation request</param>
    /// <returns>202 Accepted with operation ID for tracking</returns>
    [HttpPost("create")]
    [ProducesResponseType(typeof(AsyncOrderCreationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AsyncOrderCreationResponse>> CreateOrderAsync([FromBody] CreateAsyncOrderRequest request)
    {
        var userId = GetUserId();
        var correlationId = Guid.NewGuid();

        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();

            // Validate server plan
            var serverPlan = await _unitOfWork.ServerPlans.GetByIdAsync(request.ServerPlanId);
            if (serverPlan == null || !serverPlan.IsActive)
            {
                return BadRequest(new { message = "Invalid or inactive server plan" });
            }

            // Generate order number
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var todayOrderCount = await _unitOfWork.Orders.GetAllAsync();
            var todayOrders = todayOrderCount.Count(o => o.OrderNumber.StartsWith($"ORD-{datePrefix}"));
            var orderNumber = $"ORD-{datePrefix}-{(todayOrders + 1):D5}";

            // Create order entity
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ServerPlanId = request.ServerPlanId,
                OrderNumber = orderNumber,
                OrderType = OrderType.NewServer,
                Status = OrderStatus.Pending,
                TotalAmount = serverPlan.Price,
                Currency = serverPlan.Currency,
                PaymentMethod = request.PaymentMethod,
                BillingDetails = request.BillingDetails ?? "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Orders.AddAsync(order);

            // Create game server entity (placeholder - will be fully provisioned asynchronously)
            var gameServer = new GameServer
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ServerTypeId = serverPlan.ServerTypeId,
                ServerName = request.ServerName ?? $"{serverPlan.Name}-{Guid.NewGuid().ToString().Substring(0, 8)}",
                Status = ServerStatus.Pending,
                Configuration = "{}",
                Version = "latest",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GameServers.AddAsync(gameServer);

            // Link server to order
            order.ServerId = gameServer.Id;

            // Create async operation for tracking
            var operation = new AsyncOperation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OperationType = "ServerProvisioning",
                Status = AsyncOperationStatus.Pending,
                ResourceId = gameServer.Id,
                ResourceType = "GameServer",
                CorrelationId = correlationId,
                ProgressPercentage = 0,
                StatusMessage = "Order created, awaiting provisioning",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    OrderId = order.Id,
                    OrderNumber = orderNumber,
                    ServerPlanId = serverPlan.Id,
                    ServerPlanName = serverPlan.Name
                })
            };

            await _unitOfWork.AsyncOperations.AddAsync(operation);

            // Enqueue OrderCreated event
            await _outboxService.EnqueueAsync(new OrderCreated
            {
                CorrelationId = correlationId,
                UserId = userId,
                OrderId = order.Id,
                OrderNumber = orderNumber,
                ServerPlanId = serverPlan.Id,
                OrderType = OrderType.NewServer.ToString(),
                TotalAmount = serverPlan.Price,
                Currency = serverPlan.Currency,
                PaymentMethod = request.PaymentMethod.ToString(),
                ServerId = gameServer.Id
            });

            // Enqueue ServerProvisioningRequested event
            await _outboxService.EnqueueAsync(new ServerProvisioningRequested
            {
                CorrelationId = correlationId,
                UserId = userId,
                GameServerId = gameServer.Id,
                ServerPlanId = serverPlan.Id,
                OrderId = order.Id,
                Region = request.Region ?? "nyc3",
                CloudProvider = "DigitalOcean",
                ServerName = gameServer.ServerName,
                ServerTypeId = serverPlan.ServerTypeId
            });

            // Commit transaction (this will save entities and enqueue events in outbox)
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Created order {OrderNumber} for user {UserId} with correlation {CorrelationId}",
                orderNumber, userId, correlationId);

            // Return 202 Accepted with operation tracking information
            return Accepted(new AsyncOrderCreationResponse
            {
                OperationId = operation.Id,
                CorrelationId = correlationId,
                OrderId = order.Id,
                OrderNumber = orderNumber,
                GameServerId = gameServer.Id,
                Status = "Pending",
                Message = "Order created successfully. Server provisioning has been initiated.",
                StatusUrl = new Uri($"/api/operations/{operation.Id}", UriKind.Relative)
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while creating async order for user {UserId}", userId);

            // Rollback transaction
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch (InvalidOperationException rollbackEx)
            {
                _logger.LogError(rollbackEx, "Error rolling back transaction");
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Unexpected error rolling back transaction");
            }

            return StatusCode(500, new { message = "An invalid operation occurred while creating the order" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating async order for user {UserId}", userId);

            // Rollback transaction
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Error rolling back transaction");
            }

            return StatusCode(500, new { message = "An unexpected error occurred while creating the order" });
        }
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

    // NOTE: This is a temporary reference to context for demonstration
    // In a real implementation, AsyncOperations would be part of UnitOfWork
    private Infrastructure.Data.GameServerDbContext Context =>
        (Infrastructure.Data.GameServerDbContext)_unitOfWork.GetType()
            .GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_unitOfWork)!;
}

#region Request/Response Models

/// <summary>
/// Request model for creating an async order
/// </summary>
internal class CreateAsyncOrderRequest
{
    /// <summary>
    /// Server plan ID to order
    /// </summary>
    public Guid ServerPlanId { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Billing details (JSON)
    /// </summary>
    public string? BillingDetails { get; set; }

    /// <summary>
    /// Server name (optional, will be auto-generated if not provided)
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Region for server provisioning
    /// </summary>
    public string? Region { get; set; }
}

/// <summary>
/// Response model for async order creation
/// </summary>
internal class AsyncOrderCreationResponse
{
    /// <summary>
    /// Operation ID for tracking the async operation
    /// </summary>
    public Guid OperationId { get; set; }

    /// <summary>
    /// Correlation ID for tracking related events
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Created order ID
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Order number
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Created game server ID
    /// </summary>
    public Guid GameServerId { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// URL to check operation status
    /// </summary>
    public Uri? StatusUrl { get; set; }
}

#endregion

using GameServerHosting.Api.Data;
using GameServerHosting.Api.Models;
using GameServerHosting.Shared.Entities;
using GameServerHosting.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// Order management controller
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly GameServerDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        GameServerDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    /// <param name="request">Order creation request</param>
    /// <returns>Created order details</returns>
    [HttpPost("create")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized order creation attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            // Validate server plan exists
            var serverPlan = await _context.ServerPlans
                .Include(sp => sp.ServerType)
                .FirstOrDefaultAsync(sp => sp.Id == request.ServerPlanId);

            if (serverPlan == null)
            {
                _logger.LogWarning("Server plan not found: {ServerPlanId}", request.ServerPlanId);
                return NotFound(new { message = "Server plan not found" });
            }

            if (!serverPlan.IsActive)
            {
                _logger.LogWarning("Attempt to order inactive server plan: {ServerPlanId}", request.ServerPlanId);
                return BadRequest(new { message = "This server plan is not currently available" });
            }

            // Validate server exists if provided (for renewals/upgrades)
            GameServer? gameServer = null;
            if (request.ServerId.HasValue)
            {
                gameServer = await _context.GameServers
                    .FirstOrDefaultAsync(gs => gs.Id == request.ServerId.Value);

                if (gameServer == null)
                {
                    _logger.LogWarning("Game server not found: {ServerId}", request.ServerId.Value);
                    return NotFound(new { message = "Game server not found" });
                }

                // Ensure user owns this server
                if (gameServer.UserId != userGuid)
                {
                    _logger.LogWarning("User {UserId} attempted to create order for server {ServerId} owned by {OwnerId}",
                        userId, request.ServerId.Value, gameServer.UserId);
                    return BadRequest(new { message = "You don't have permission to manage this server" });
                }
            }

            // Generate order number (format: ORD-YYYYMMDD-NNNNN)
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var todayOrderCount = await _context.Orders
                .Where(o => o.OrderNumber.StartsWith($"ORD-{datePrefix}"))
                .CountAsync();
            var orderNumber = $"ORD-{datePrefix}-{(todayOrderCount + 1):D5}";

            // Create order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userGuid,
                ServerId = request.ServerId,
                ServerPlanId = request.ServerPlanId,
                OrderNumber = orderNumber,
                OrderType = request.OrderType,
                Status = OrderStatus.Pending,
                TotalAmount = serverPlan.Price,
                Currency = serverPlan.Currency,
                PaymentMethod = request.PaymentMethod,
                BillingDetails = request.BillingDetails ?? "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created order {OrderNumber} for user {UserId}", orderNumber, userId);

            // Create response DTO
            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderType = order.OrderType,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                PaymentMethod = order.PaymentMethod,
                PaymentDate = order.PaymentDate,
                CreatedAt = order.CreatedAt,
                ServerName = gameServer?.ServerName,
                GameName = serverPlan.ServerType.GameName,
                PlanName = serverPlan.Name
            };

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { message = "An error occurred while creating the order" });
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized order retrieval attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            var order = await _context.Orders
                .Include(o => o.GameServer)
                .Include(o => o.ServerPlan)
                    .ThenInclude(sp => sp.ServerType)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", id);
                return NotFound(new { message = "Order not found" });
            }

            // Check if user owns this order
            if (order.UserId != userGuid)
            {
                _logger.LogWarning("User {UserId} attempted to access order {OrderId} owned by {OwnerId}",
                    userId, id, order.UserId);
                return StatusCode(403, new { message = "You don't have permission to view this order" });
            }

            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderType = order.OrderType,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                PaymentMethod = order.PaymentMethod,
                PaymentDate = order.PaymentDate,
                CreatedAt = order.CreatedAt,
                ServerName = order.GameServer?.ServerName,
                GameName = order.ServerPlan?.ServerType?.GameName,
                PlanName = order.ServerPlan?.Name
            };

            _logger.LogInformation("Retrieved order {OrderId} for user {UserId}", id, userId);
            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the order" });
        }
    }

    /// <summary>
    /// Get all orders for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of orders for the user</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByUserId(Guid userId)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized orders retrieval attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var currentUserGuid = Guid.Parse(currentUserId);

            // Users can only view their own orders
            if (userId != currentUserGuid)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to access orders for user {UserId}",
                    currentUserId, userId);
                return StatusCode(403, new { message = "You don't have permission to view these orders" });
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.GameServer)
                .Include(o => o.ServerPlan)
                    .ThenInclude(sp => sp.ServerType)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderType = o.OrderType,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    Currency = o.Currency,
                    PaymentMethod = o.PaymentMethod,
                    PaymentDate = o.PaymentDate,
                    CreatedAt = o.CreatedAt,
                    ServerName = o.GameServer != null ? o.GameServer.ServerName : null,
                    GameName = o.ServerPlan != null && o.ServerPlan.ServerType != null ? o.ServerPlan.ServerType.GameName : null,
                    PlanName = o.ServerPlan != null ? o.ServerPlan.Name : null
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} orders for user {UserId}", orders.Count, userId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated order details</returns>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized order status update attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            var order = await _context.Orders
                .Include(o => o.GameServer)
                .Include(o => o.ServerPlan)
                    .ThenInclude(sp => sp.ServerType)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order not found for status update: {OrderId}", id);
                return NotFound(new { message = "Order not found" });
            }

            // Check if user owns this order
            if (order.UserId != userGuid)
            {
                _logger.LogWarning("User {UserId} attempted to update order {OrderId} owned by {OwnerId}",
                    userId, id, order.UserId);
                return StatusCode(403, new { message = "You don't have permission to update this order" });
            }

            // Validate status transition
            if (order.Status == OrderStatus.Completed && request.Status != OrderStatus.Refunded)
            {
                _logger.LogWarning("Invalid status transition from Completed to {Status} for order {OrderId}",
                    request.Status, id);
                return BadRequest(new { message = "Completed orders can only be refunded" });
            }

            if (order.Status == OrderStatus.Failed && request.Status == OrderStatus.Completed)
            {
                _logger.LogWarning("Invalid status transition from Failed to Completed for order {OrderId}", id);
                return BadRequest(new { message = "Failed orders cannot be marked as completed" });
            }

            // Update order status
            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            // Update payment date if status is completed
            if (request.Status == OrderStatus.Completed && !order.PaymentDate.HasValue)
            {
                order.PaymentDate = DateTime.UtcNow;
            }

            // Update payment transaction ID if provided
            if (!string.IsNullOrEmpty(request.PaymentTransactionId))
            {
                order.PaymentTransactionId = request.PaymentTransactionId;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated order {OrderId} status to {Status}", id, request.Status);

            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderType = order.OrderType,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                PaymentMethod = order.PaymentMethod,
                PaymentDate = order.PaymentDate,
                CreatedAt = order.CreatedAt,
                ServerName = order.GameServer?.ServerName,
                GameName = order.ServerPlan?.ServerType?.GameName,
                PlanName = order.ServerPlan?.Name
            };

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId} status", id);
            return StatusCode(500, new { message = "An error occurred while updating order status" });
        }
    }
}

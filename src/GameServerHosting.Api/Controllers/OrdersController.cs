using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
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
    private readonly IOrderValidationService _orderValidationService;
    private readonly IInvoiceService _invoiceService;

    public OrdersController(
        GameServerDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<OrdersController> logger,
        IOrderValidationService orderValidationService,
        IInvoiceService invoiceService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _orderValidationService = orderValidationService;
        _invoiceService = invoiceService;
    }

    /// <summary>
    /// Create orders from cart items with full validation
    /// </summary>
    /// <param name="request">Order creation request with billing details</param>
    /// <returns>Created order details</returns>
    [HttpPost("create-from-cart")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrderDto>> CreateOrderFromCart([FromBody] CreateOrderFromCartRequest request)
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

            // Step 1: Validate cart items
            _logger.LogInformation("Validating cart items for user {UserId}", userId);
            var cartValidation = await _orderValidationService.ValidateCartItemsAsync(userGuid);
            if (!cartValidation.IsValid)
            {
                _logger.LogWarning("Cart validation failed for user {UserId}: {Error}", userId, cartValidation.ErrorMessage);
                return BadRequest(new { message = cartValidation.ErrorMessage });
            }

            // Step 2: Validate billing information
            _logger.LogInformation("Validating billing information for user {UserId}", userId);
            var billingValidation = _orderValidationService.ValidateBillingInformation(request.BillingDetails);
            if (!billingValidation.IsValid)
            {
                _logger.LogWarning("Billing validation failed for user {UserId}: {Errors}", userId, string.Join(", ", billingValidation.Errors));
                return BadRequest(new 
                { 
                    message = billingValidation.ErrorMessage,
                    errors = billingValidation.Errors 
                });
            }

            // Step 3: Check inventory/availability
            _logger.LogInformation("Checking inventory availability for user {UserId}", userId);
            var inventoryValidation = await _orderValidationService.CheckInventoryAvailabilityAsync(cartValidation.CartItems);
            if (!inventoryValidation.IsValid)
            {
                _logger.LogWarning("Inventory check failed for user {UserId}: {Errors}", userId, string.Join(", ", inventoryValidation.Errors));
                return BadRequest(new 
                { 
                    message = inventoryValidation.ErrorMessage,
                    errors = inventoryValidation.Errors 
                });
            }

            // Step 4: Create orders (one order per cart item for better tracking)
            var orders = new List<Order>();
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var todayOrderCount = await _context.Orders
                .Where(o => o.OrderNumber.StartsWith($"ORD-{datePrefix}"))
                .CountAsync();

            foreach (var cartItem in cartValidation.CartItems)
            {
                var orderNumber = $"ORD-{datePrefix}-{(todayOrderCount + orders.Count + 1):D5}";

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userGuid,
                    ServerId = null, // Will be set when server is provisioned
                    ServerPlanId = cartItem.ServerPlanId,
                    OrderNumber = orderNumber,
                    OrderType = OrderType.NewServer,
                    Status = OrderStatus.Pending,
                    TotalAmount = cartItem.Price * cartItem.Quantity,
                    Currency = cartItem.Currency,
                    PaymentMethod = request.PaymentMethod,
                    BillingDetails = request.BillingDetails,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                orders.Add(order);
                _context.Orders.Add(order);
            }

            // Save all orders
            await _context.SaveChangesAsync();

            // Step 5: Clear cart after successful order creation
            _context.CartItems.RemoveRange(cartValidation.CartItems);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created {OrderCount} orders for user {UserId}, total: {Total} {Currency}",
                orders.Count, userId, cartValidation.TotalAmount, cartValidation.Currency);

            // Return the first order (or a summary of all orders)
            // For simplicity, returning info about the first order
            var firstOrder = orders.First();
            var firstCartItem = cartValidation.CartItems.First();

            var orderDto = new OrderDto
            {
                Id = firstOrder.Id,
                OrderNumber = firstOrder.OrderNumber,
                OrderType = firstOrder.OrderType,
                Status = firstOrder.Status,
                TotalAmount = cartValidation.TotalAmount, // Total of all orders
                Currency = firstOrder.Currency,
                PaymentMethod = firstOrder.PaymentMethod,
                PaymentDate = firstOrder.PaymentDate,
                CreatedAt = firstOrder.CreatedAt,
                ServerName = null,
                GameName = firstCartItem.ServerPlan?.ServerType?.GameName,
                PlanName = firstCartItem.ServerPlan?.Name
            };

            return CreatedAtAction(nameof(GetOrderById), new { id = firstOrder.Id }, orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order from cart");
            return StatusCode(500, new { message = "An error occurred while creating the order" });
        }
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
                    .ThenInclude(sp => sp!.ServerType)
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
                    .ThenInclude(sp => sp!.ServerType)
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
                    .ThenInclude(sp => sp!.ServerType)
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

    /// <summary>
    /// Get invoice/receipt for an order as HTML
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Invoice HTML</returns>
    [HttpGet("{id}/invoice")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("text/html")]
    public async Task<IActionResult> GetOrderInvoice(Guid id)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized invoice retrieval attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            // Verify order exists and belongs to user
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order not found for invoice: {OrderId}", id);
                return NotFound(new { message = "Order not found" });
            }

            if (order.UserId != userGuid)
            {
                _logger.LogWarning("User {UserId} attempted to access invoice for order {OrderId} owned by {OwnerId}",
                    userId, id, order.UserId);
                return StatusCode(403, new { message = "You don't have permission to view this invoice" });
            }

            // Generate invoice HTML
            var invoiceHtml = await _invoiceService.GenerateInvoiceHtmlAsync(id);

            _logger.LogInformation("Generated invoice for order {OrderId} for user {UserId}", id, userId);
            
            return Content(invoiceHtml, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while generating the invoice" });
        }
    }

    /// <summary>
    /// Download invoice/receipt for an order as a file
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Invoice file (HTML)</returns>
    [HttpGet("{id}/invoice/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadOrderInvoice(Guid id)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized invoice download attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            // Verify order exists and belongs to user
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order not found for invoice download: {OrderId}", id);
                return NotFound(new { message = "Order not found" });
            }

            if (order.UserId != userGuid)
            {
                _logger.LogWarning("User {UserId} attempted to download invoice for order {OrderId} owned by {OwnerId}",
                    userId, id, order.UserId);
                return StatusCode(403, new { message = "You don't have permission to download this invoice" });
            }

            // Generate invoice
            var invoiceBytes = await _invoiceService.GenerateInvoiceAsync(id);
            var fileName = $"Invoice-{order.OrderNumber}.html";

            _logger.LogInformation("Downloaded invoice for order {OrderId} for user {UserId}", id, userId);
            
            return File(invoiceBytes, "text/html", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading invoice for order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while downloading the invoice" });
        }
    }
}

using GameServerHosting.Api.Data;
using GameServerHosting.Api.Models;
using GameServerHosting.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// Shopping cart management controller
/// </summary>
[ApiController]
[Route("api/cart")]
[Produces("application/json")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly GameServerDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CartController> _logger;

    public CartController(
        GameServerDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<CartController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's shopping cart
    /// </summary>
    /// <returns>List of cart items with server plan details</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CartItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<CartItemDto>>> GetCart()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized cart access attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userGuid)
                .Include(ci => ci.ServerPlan)
                    .ThenInclude(sp => sp.ServerType)
                .OrderBy(ci => ci.CreatedAt)
                .Select(ci => new CartItemDto
                {
                    Id = ci.Id,
                    UserId = ci.UserId,
                    ServerPlanId = ci.ServerPlanId,
                    Quantity = ci.Quantity,
                    Price = ci.Price,
                    Currency = ci.Currency,
                    CreatedAt = ci.CreatedAt,
                    UpdatedAt = ci.UpdatedAt,
                    ServerPlan = new ServerPlanDto
                    {
                        Id = ci.ServerPlan.Id,
                        ServerTypeId = ci.ServerPlan.ServerTypeId,
                        Name = ci.ServerPlan.Name,
                        BillingCycle = ci.ServerPlan.BillingCycle,
                        Price = ci.ServerPlan.Price,
                        Currency = ci.ServerPlan.Currency,
                        DurationHours = ci.ServerPlan.DurationHours,
                        DiscountPercentage = ci.ServerPlan.DiscountPercentage,
                        IsActive = ci.ServerPlan.IsActive,
                        DisplayOrder = ci.ServerPlan.DisplayOrder,
                        Description = ci.ServerPlan.Description,
                        CreatedAt = ci.ServerPlan.CreatedAt,
                        UpdatedAt = ci.ServerPlan.UpdatedAt
                    }
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} cart items for user {UserId}", cartItems.Count, userId);
            return Ok(cartItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart");
            return StatusCode(500, new { message = "An error occurred while retrieving cart" });
        }
    }

    /// <summary>
    /// Add item to shopping cart
    /// </summary>
    /// <param name="request">Add to cart request</param>
    /// <returns>Updated or created cart item</returns>
    [HttpPost("add")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItemDto>> AddToCart([FromBody] AddToCartRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized add to cart attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            // Check if server plan exists and is active
            var serverPlan = await _context.ServerPlans.FindAsync(request.ServerPlanId);
            if (serverPlan == null)
            {
                _logger.LogWarning("Server plan not found: {ServerPlanId}", request.ServerPlanId);
                return NotFound(new { message = "Server plan not found" });
            }

            if (!serverPlan.IsActive)
            {
                _logger.LogWarning("Attempt to add inactive server plan to cart: {ServerPlanId}", request.ServerPlanId);
                return BadRequest(new { message = "This server plan is not currently available" });
            }

            // Check if item already exists in cart
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userGuid && ci.ServerPlanId == request.ServerPlanId);

            CartItem cartItem;

            if (existingCartItem != null)
            {
                // Update quantity
                existingCartItem.Quantity += request.Quantity;
                existingCartItem.Price = serverPlan.Price; // Update price to current price
                existingCartItem.UpdatedAt = DateTime.UtcNow;
                cartItem = existingCartItem;
                _logger.LogInformation("Updated cart item quantity for user {UserId}, plan {PlanId}", userId, request.ServerPlanId);
            }
            else
            {
                // Create new cart item
                cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userGuid,
                    ServerPlanId = request.ServerPlanId,
                    Quantity = request.Quantity,
                    Price = serverPlan.Price,
                    Currency = serverPlan.Currency,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
                _logger.LogInformation("Added new cart item for user {UserId}, plan {PlanId}", userId, request.ServerPlanId);
            }

            await _context.SaveChangesAsync();

            // Reload with server plan details
            var savedCartItem = await _context.CartItems
                .Include(ci => ci.ServerPlan)
                .FirstAsync(ci => ci.Id == cartItem.Id);

            var dto = new CartItemDto
            {
                Id = savedCartItem.Id,
                UserId = savedCartItem.UserId,
                ServerPlanId = savedCartItem.ServerPlanId,
                Quantity = savedCartItem.Quantity,
                Price = savedCartItem.Price,
                Currency = savedCartItem.Currency,
                CreatedAt = savedCartItem.CreatedAt,
                UpdatedAt = savedCartItem.UpdatedAt,
                ServerPlan = new ServerPlanDto
                {
                    Id = savedCartItem.ServerPlan.Id,
                    ServerTypeId = savedCartItem.ServerPlan.ServerTypeId,
                    Name = savedCartItem.ServerPlan.Name,
                    BillingCycle = savedCartItem.ServerPlan.BillingCycle,
                    Price = savedCartItem.ServerPlan.Price,
                    Currency = savedCartItem.ServerPlan.Currency,
                    DurationHours = savedCartItem.ServerPlan.DurationHours,
                    DiscountPercentage = savedCartItem.ServerPlan.DiscountPercentage,
                    IsActive = savedCartItem.ServerPlan.IsActive,
                    DisplayOrder = savedCartItem.ServerPlan.DisplayOrder,
                    Description = savedCartItem.ServerPlan.Description,
                    CreatedAt = savedCartItem.ServerPlan.CreatedAt,
                    UpdatedAt = savedCartItem.ServerPlan.UpdatedAt
                }
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return StatusCode(500, new { message = "An error occurred while adding to cart" });
        }
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    /// <param name="itemId">Cart item ID</param>
    /// <param name="request">Update request with new quantity</param>
    /// <returns>Updated cart item</returns>
    [HttpPut("update/{itemId}")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItemDto>> UpdateCartItem(Guid itemId, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized cart update attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            var cartItem = await _context.CartItems
                .Include(ci => ci.ServerPlan)
                .FirstOrDefaultAsync(ci => ci.Id == itemId);

            if (cartItem == null)
            {
                _logger.LogWarning("Cart item not found: {ItemId}", itemId);
                return NotFound(new { message = "Cart item not found" });
            }

            // Check if user owns this cart item
            if (cartItem.UserId != userGuid)
            {
                _logger.LogWarning("User {UserId} attempted to update cart item {ItemId} owned by {OwnerId}", 
                    userId, itemId, cartItem.UserId);
                return StatusCode(403, new { message = "You don't have permission to update this cart item" });
            }

            // Update quantity
            cartItem.Quantity = request.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dto = new CartItemDto
            {
                Id = cartItem.Id,
                UserId = cartItem.UserId,
                ServerPlanId = cartItem.ServerPlanId,
                Quantity = cartItem.Quantity,
                Price = cartItem.Price,
                Currency = cartItem.Currency,
                CreatedAt = cartItem.CreatedAt,
                UpdatedAt = cartItem.UpdatedAt,
                ServerPlan = new ServerPlanDto
                {
                    Id = cartItem.ServerPlan.Id,
                    ServerTypeId = cartItem.ServerPlan.ServerTypeId,
                    Name = cartItem.ServerPlan.Name,
                    BillingCycle = cartItem.ServerPlan.BillingCycle,
                    Price = cartItem.ServerPlan.Price,
                    Currency = cartItem.ServerPlan.Currency,
                    DurationHours = cartItem.ServerPlan.DurationHours,
                    DiscountPercentage = cartItem.ServerPlan.DiscountPercentage,
                    IsActive = cartItem.ServerPlan.IsActive,
                    DisplayOrder = cartItem.ServerPlan.DisplayOrder,
                    Description = cartItem.ServerPlan.Description,
                    CreatedAt = cartItem.ServerPlan.CreatedAt,
                    UpdatedAt = cartItem.ServerPlan.UpdatedAt
                }
            };

            _logger.LogInformation("Updated cart item {ItemId} for user {UserId}", itemId, userId);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item {ItemId}", itemId);
            return StatusCode(500, new { message = "An error occurred while updating cart item" });
        }
    }

    /// <summary>
    /// Remove item from shopping cart
    /// </summary>
    /// <param name="itemId">Cart item ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("remove/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveFromCart(Guid itemId)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized cart remove attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            var cartItem = await _context.CartItems.FindAsync(itemId);
            if (cartItem == null)
            {
                _logger.LogWarning("Cart item not found for removal: {ItemId}", itemId);
                return NotFound(new { message = "Cart item not found" });
            }

            // Check if user owns this cart item
            if (cartItem.UserId != userGuid)
            {
                _logger.LogWarning("User {UserId} attempted to remove cart item {ItemId} owned by {OwnerId}", 
                    userId, itemId, cartItem.UserId);
                return StatusCode(403, new { message = "You don't have permission to remove this cart item" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed cart item {ItemId} for user {UserId}", itemId, userId);
            return Ok(new { message = "Item removed from cart successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item {ItemId}", itemId);
            return StatusCode(500, new { message = "An error occurred while removing cart item" });
        }
    }

    /// <summary>
    /// Clear all items from shopping cart
    /// </summary>
    /// <returns>Success message</returns>
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ClearCart()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized cart clear attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userGuid)
                .ToListAsync();

            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleared {Count} cart items for user {UserId}", cartItems.Count, userId);
            }

            return Ok(new { message = "Cart cleared successfully", itemsRemoved = cartItems.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(500, new { message = "An error occurred while clearing cart" });
        }
    }
}

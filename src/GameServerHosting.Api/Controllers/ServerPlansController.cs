using GameServerHosting.Application.DTOs;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// Server plan management controller
/// </summary>
[ApiController]
[Route("api/server-plans")]
[Produces("application/json")]
public class ServerPlansController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ServerPlansController> _logger;

    public ServerPlansController(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ILogger<ServerPlansController> logger)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all server plans
    /// </summary>
    /// <param name="isActive">Optional filter for active/inactive server plans</param>
    /// <returns>List of server plans</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServerPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ServerPlanDto>>> GetServerPlans(
        [FromQuery] bool? isActive = null)
    {
        try
        {
            IEnumerable<ServerPlan> serverPlans;
            
            if (isActive.HasValue)
            {
                serverPlans = await _unitOfWork.ServerPlans.FindAsync(sp => sp.IsActive == isActive.Value);
                serverPlans = serverPlans.OrderBy(sp => sp.DisplayOrder).ThenBy(sp => sp.Name);
            }
            else
            {
                serverPlans = await _unitOfWork.ServerPlans.GetAllAsync();
                serverPlans = serverPlans.OrderBy(sp => sp.DisplayOrder).ThenBy(sp => sp.Name);
            }

            var serverPlanDtos = serverPlans.Select(sp => new ServerPlanDto
            {
                Id = sp.Id,
                ServerTypeId = sp.ServerTypeId,
                Name = sp.Name,
                BillingCycle = sp.BillingCycle,
                Price = sp.Price,
                Currency = sp.Currency,
                DurationHours = sp.DurationHours,
                DiscountPercentage = sp.DiscountPercentage,
                IsActive = sp.IsActive,
                DisplayOrder = sp.DisplayOrder,
                Description = sp.Description,
                CreatedAt = sp.CreatedAt,
                UpdatedAt = sp.UpdatedAt
            }).ToList();

            _logger.LogInformation("Retrieved {Count} server plans", serverPlanDtos.Count);
            return Ok(serverPlanDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving server plans");
            return StatusCode(500, new { message = "An error occurred while retrieving server plans" });
        }
    }

    /// <summary>
    /// Get a specific server plan by ID
    /// </summary>
    /// <param name="id">Server plan ID</param>
    /// <returns>Server plan details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServerPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServerPlanDto>> GetServerPlan(Guid id)
    {
        try
        {
            var serverPlan = await _unitOfWork.ServerPlans.GetByIdAsync(id);

            if (serverPlan == null)
            {
                _logger.LogWarning("Server plan not found: {Id}", id);
                return NotFound(new { message = "Server plan not found" });
            }

            var serverPlanDto = new ServerPlanDto
            {
                Id = serverPlan.Id,
                ServerTypeId = serverPlan.ServerTypeId,
                Name = serverPlan.Name,
                BillingCycle = serverPlan.BillingCycle,
                Price = serverPlan.Price,
                Currency = serverPlan.Currency,
                DurationHours = serverPlan.DurationHours,
                DiscountPercentage = serverPlan.DiscountPercentage,
                IsActive = serverPlan.IsActive,
                DisplayOrder = serverPlan.DisplayOrder,
                Description = serverPlan.Description,
                CreatedAt = serverPlan.CreatedAt,
                UpdatedAt = serverPlan.UpdatedAt
            };

            _logger.LogInformation("Retrieved server plan: {Id}", id);
            return Ok(serverPlanDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving server plan {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving server plan" });
        }
    }

    /// <summary>
    /// Get server plans by server type ID
    /// </summary>
    /// <param name="typeId">Server type ID</param>
    /// <param name="isActive">Optional filter for active/inactive server plans</param>
    /// <returns>List of server plans for the specified server type</returns>
    [HttpGet("by-type/{typeId}")]
    [ProducesResponseType(typeof(IEnumerable<ServerPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ServerPlanDto>>> GetServerPlansByType(
        Guid typeId,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            // Check if server type exists
            var serverTypeExists = await _unitOfWork.ServerTypes.AnyAsync(st => st.Id == typeId);
            if (!serverTypeExists)
            {
                _logger.LogWarning("Server type not found: {TypeId}", typeId);
                return NotFound(new { message = "Server type not found" });
            }

            var serverPlans = await _unitOfWork.ServerPlans.GetByServerTypeIdAsync(typeId, isActive);

            var serverPlanDtos = serverPlans.Select(sp => new ServerPlanDto
            {
                Id = sp.Id,
                ServerTypeId = sp.ServerTypeId,
                Name = sp.Name,
                BillingCycle = sp.BillingCycle,
                Price = sp.Price,
                Currency = sp.Currency,
                DurationHours = sp.DurationHours,
                DiscountPercentage = sp.DiscountPercentage,
                IsActive = sp.IsActive,
                DisplayOrder = sp.DisplayOrder,
                Description = sp.Description,
                CreatedAt = sp.CreatedAt,
                UpdatedAt = sp.UpdatedAt
            }).ToList();

            _logger.LogInformation("Retrieved {Count} server plans for server type {TypeId}", serverPlanDtos.Count, typeId);
            return Ok(serverPlanDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving server plans for server type {TypeId}", typeId);
            return StatusCode(500, new { message = "An error occurred while retrieving server plans" });
        }
    }

    /// <summary>
    /// Create a new server plan (Admin only)
    /// </summary>
    /// <param name="request">Server plan creation request</param>
    /// <returns>Created server plan</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ServerPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ServerPlanDto>> CreateServerPlan([FromBody] CreateServerPlanRequest request)
    {
        try
        {
            // Check if user is admin
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized server plan creation attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role != UserRole.Admin)
            {
                _logger.LogWarning("Non-admin user {UserId} attempted to create server plan", userId);
                return StatusCode(403, new { message = "Only administrators can create server plans" });
            }

            // Check if server type exists
            var serverTypeExists = await _unitOfWork.ServerTypes.AnyAsync(st => st.Id == request.ServerTypeId);
            if (!serverTypeExists)
            {
                _logger.LogWarning("Server type not found: {ServerTypeId}", request.ServerTypeId);
                return BadRequest(new { message = "Server type not found" });
            }

            // Check if a plan with the same name already exists for this server type
            var existingPlan = await _unitOfWork.ServerPlans.ExistsWithNameAsync(request.ServerTypeId, request.Name);

            if (existingPlan)
            {
                _logger.LogWarning("Server plan with name {Name} already exists for server type {ServerTypeId}", 
                    request.Name, request.ServerTypeId);
                return BadRequest(new { message = "A server plan with this name already exists for this server type" });
            }

            var serverPlan = new ServerPlan
            {
                Id = Guid.NewGuid(),
                ServerTypeId = request.ServerTypeId,
                Name = request.Name,
                BillingCycle = request.BillingCycle,
                Price = request.Price,
                Currency = request.Currency,
                DurationHours = request.DurationHours,
                DiscountPercentage = request.DiscountPercentage,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ServerPlans.AddAsync(serverPlan);
            await _unitOfWork.SaveChangesAsync();

            var dto = new ServerPlanDto
            {
                Id = serverPlan.Id,
                ServerTypeId = serverPlan.ServerTypeId,
                Name = serverPlan.Name,
                BillingCycle = serverPlan.BillingCycle,
                Price = serverPlan.Price,
                Currency = serverPlan.Currency,
                DurationHours = serverPlan.DurationHours,
                DiscountPercentage = serverPlan.DiscountPercentage,
                IsActive = serverPlan.IsActive,
                DisplayOrder = serverPlan.DisplayOrder,
                Description = serverPlan.Description,
                CreatedAt = serverPlan.CreatedAt,
                UpdatedAt = serverPlan.UpdatedAt
            };

            _logger.LogInformation("Server plan created: {Name} for server type {ServerTypeId} by user {UserId}", 
                serverPlan.Name, serverPlan.ServerTypeId, userId);
            return CreatedAtAction(nameof(GetServerPlan), new { id = serverPlan.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating server plan");
            return StatusCode(500, new { message = "An error occurred while creating server plan" });
        }
    }

    /// <summary>
    /// Update an existing server plan (Admin only)
    /// </summary>
    /// <param name="id">Server plan ID</param>
    /// <param name="request">Server plan update request</param>
    /// <returns>Updated server plan</returns>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ServerPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServerPlanDto>> UpdateServerPlan(Guid id, [FromBody] UpdateServerPlanRequest request)
    {
        try
        {
            // Check if user is admin
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized server plan update attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role != UserRole.Admin)
            {
                _logger.LogWarning("Non-admin user {UserId} attempted to update server plan", userId);
                return StatusCode(403, new { message = "Only administrators can update server plans" });
            }

            var serverPlan = await _unitOfWork.ServerPlans.GetByIdAsync(id);
            if (serverPlan == null)
            {
                _logger.LogWarning("Server plan not found for update: {Id}", id);
                return NotFound(new { message = "Server plan not found" });
            }

            // Check if another plan with same name already exists for this server type
            var existingPlan = await _unitOfWork.ServerPlans.ExistsWithNameAsync(
                serverPlan.ServerTypeId, 
                request.Name, 
                id);

            if (existingPlan)
            {
                _logger.LogWarning("Server plan with name {Name} already exists for server type {ServerTypeId}", 
                    request.Name, serverPlan.ServerTypeId);
                return BadRequest(new { message = "A server plan with this name already exists for this server type" });
            }

            // Update properties
            serverPlan.Name = request.Name;
            serverPlan.BillingCycle = request.BillingCycle;
            serverPlan.Price = request.Price;
            serverPlan.Currency = request.Currency;
            serverPlan.DurationHours = request.DurationHours;
            serverPlan.DiscountPercentage = request.DiscountPercentage;
            serverPlan.IsActive = request.IsActive;
            serverPlan.DisplayOrder = request.DisplayOrder;
            serverPlan.Description = request.Description;
            serverPlan.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.ServerPlans.Update(serverPlan);
            await _unitOfWork.SaveChangesAsync();

            var dto = new ServerPlanDto
            {
                Id = serverPlan.Id,
                ServerTypeId = serverPlan.ServerTypeId,
                Name = serverPlan.Name,
                BillingCycle = serverPlan.BillingCycle,
                Price = serverPlan.Price,
                Currency = serverPlan.Currency,
                DurationHours = serverPlan.DurationHours,
                DiscountPercentage = serverPlan.DiscountPercentage,
                IsActive = serverPlan.IsActive,
                DisplayOrder = serverPlan.DisplayOrder,
                Description = serverPlan.Description,
                CreatedAt = serverPlan.CreatedAt,
                UpdatedAt = serverPlan.UpdatedAt
            };

            _logger.LogInformation("Server plan updated: {Id} by user {UserId}", id, userId);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating server plan {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating server plan" });
        }
    }

    /// <summary>
    /// Delete a server plan (Admin only)
    /// </summary>
    /// <param name="id">Server plan ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteServerPlan(Guid id)
    {
        try
        {
            // Check if user is admin
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized server plan deletion attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role != UserRole.Admin)
            {
                _logger.LogWarning("Non-admin user {UserId} attempted to delete server plan", userId);
                return StatusCode(403, new { message = "Only administrators can delete server plans" });
            }

            // Get server plan with its orders to check if it's in use
            var serverPlan = await _unitOfWork.ServerPlans.GetByIdAsync(id);
            if (serverPlan != null)
            {
                var orders = await _unitOfWork.Orders.FindAsync(o => o.ServerPlanId == id);
                if (orders.Any())
                {
                    _logger.LogWarning("Cannot delete server plan {Id} - it is in use", id);
                    return BadRequest(new { message = "Cannot delete server plan that is in use by orders" });
                }
            }

            if (serverPlan == null)
            {
                _logger.LogWarning("Server plan not found for deletion: {Id}", id);
                return NotFound(new { message = "Server plan not found" });
            }

            _unitOfWork.ServerPlans.Remove(serverPlan);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Server plan deleted: {Id} by user {UserId}", id, userId);
            return Ok(new { message = "Server plan deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting server plan {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting server plan" });
        }
    }
}

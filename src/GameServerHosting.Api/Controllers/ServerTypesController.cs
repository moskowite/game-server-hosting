using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// Server type management controller
/// </summary>
[ApiController]
[Route("api/server-types")]
[Produces("application/json")]
public class ServerTypesController : ControllerBase
{
    private readonly GameServerDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ServerTypesController> _logger;

    public ServerTypesController(
        GameServerDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<ServerTypesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all server types
    /// </summary>
    /// <param name="isActive">Optional filter for active/inactive server types</param>
    /// <param name="gameName">Optional filter by game name</param>
    /// <returns>List of server types</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServerTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ServerTypeDto>>> GetServerTypes(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? gameName = null)
    {
        try
        {
            var query = _context.ServerTypes.AsQueryable();

            // Apply filters
            if (isActive.HasValue)
            {
                query = query.Where(st => st.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(gameName))
            {
                query = query.Where(st => st.GameName.ToLower() == gameName.ToLower());
            }

            var serverTypes = await query
                .OrderBy(st => st.GameName)
                .ThenBy(st => st.Name)
                .Select(st => new ServerTypeDto
                {
                    Id = st.Id,
                    Name = st.Name,
                    GameName = st.GameName,
                    Description = st.Description,
                    CpuCores = st.CpuCores,
                    RamMb = st.RamMb,
                    DiskGb = st.DiskGb,
                    MaxPlayers = st.MaxPlayers,
                    HourlyPrice = st.HourlyPrice,
                    MonthlyPrice = st.MonthlyPrice,
                    DockerImage = st.DockerImage,
                    DefaultConfig = st.DefaultConfig,
                    IsActive = st.IsActive,
                    CreatedAt = st.CreatedAt,
                    UpdatedAt = st.UpdatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} server types", serverTypes.Count);
            return Ok(serverTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving server types");
            return StatusCode(500, new { message = "An error occurred while retrieving server types" });
        }
    }

    /// <summary>
    /// Get a specific server type by ID
    /// </summary>
    /// <param name="id">Server type ID</param>
    /// <returns>Server type details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServerTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServerTypeDto>> GetServerType(Guid id)
    {
        try
        {
            var serverType = await _context.ServerTypes
                .Where(st => st.Id == id)
                .Select(st => new ServerTypeDto
                {
                    Id = st.Id,
                    Name = st.Name,
                    GameName = st.GameName,
                    Description = st.Description,
                    CpuCores = st.CpuCores,
                    RamMb = st.RamMb,
                    DiskGb = st.DiskGb,
                    MaxPlayers = st.MaxPlayers,
                    HourlyPrice = st.HourlyPrice,
                    MonthlyPrice = st.MonthlyPrice,
                    DockerImage = st.DockerImage,
                    DefaultConfig = st.DefaultConfig,
                    IsActive = st.IsActive,
                    CreatedAt = st.CreatedAt,
                    UpdatedAt = st.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (serverType == null)
            {
                _logger.LogWarning("Server type not found: {Id}", id);
                return NotFound(new { message = "Server type not found" });
            }

            _logger.LogInformation("Retrieved server type: {Id}", id);
            return Ok(serverType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving server type {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving server type" });
        }
    }

    /// <summary>
    /// Create a new server type (Admin only)
    /// </summary>
    /// <param name="request">Server type creation request</param>
    /// <returns>Created server type</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ServerTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ServerTypeDto>> CreateServerType([FromBody] CreateServerTypeRequest request)
    {
        try
        {
            // Check if user is admin
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized server type creation attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role != UserRole.Admin)
            {
                _logger.LogWarning("Non-admin user {UserId} attempted to create server type", userId);
                return StatusCode(403, new { message = "Only administrators can create server types" });
            }

            // Check if server type with same name already exists
            var existingServerType = await _context.ServerTypes
                .FirstOrDefaultAsync(st => st.Name.ToLower() == request.Name.ToLower());

            if (existingServerType != null)
            {
                _logger.LogWarning("Server type with name {Name} already exists", request.Name);
                return BadRequest(new { message = "A server type with this name already exists" });
            }

            var serverType = new ServerType
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                GameName = request.GameName,
                Description = request.Description,
                CpuCores = request.CpuCores,
                RamMb = request.RamMb,
                DiskGb = request.DiskGb,
                MaxPlayers = request.MaxPlayers,
                HourlyPrice = request.HourlyPrice,
                MonthlyPrice = request.MonthlyPrice,
                DockerImage = request.DockerImage,
                DefaultConfig = request.DefaultConfig,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ServerTypes.Add(serverType);
            await _context.SaveChangesAsync();

            var dto = new ServerTypeDto
            {
                Id = serverType.Id,
                Name = serverType.Name,
                GameName = serverType.GameName,
                Description = serverType.Description,
                CpuCores = serverType.CpuCores,
                RamMb = serverType.RamMb,
                DiskGb = serverType.DiskGb,
                MaxPlayers = serverType.MaxPlayers,
                HourlyPrice = serverType.HourlyPrice,
                MonthlyPrice = serverType.MonthlyPrice,
                DockerImage = serverType.DockerImage,
                DefaultConfig = serverType.DefaultConfig,
                IsActive = serverType.IsActive,
                CreatedAt = serverType.CreatedAt,
                UpdatedAt = serverType.UpdatedAt
            };

            _logger.LogInformation("Server type created: {Name} by user {UserId}", serverType.Name, userId);
            return CreatedAtAction(nameof(GetServerType), new { id = serverType.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating server type");
            return StatusCode(500, new { message = "An error occurred while creating server type" });
        }
    }

    /// <summary>
    /// Update an existing server type (Admin only)
    /// </summary>
    /// <param name="id">Server type ID</param>
    /// <param name="request">Server type update request</param>
    /// <returns>Updated server type</returns>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ServerTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServerTypeDto>> UpdateServerType(Guid id, [FromBody] UpdateServerTypeRequest request)
    {
        try
        {
            // Check if user is admin
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized server type update attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role != UserRole.Admin)
            {
                _logger.LogWarning("Non-admin user {UserId} attempted to update server type", userId);
                return StatusCode(403, new { message = "Only administrators can update server types" });
            }

            var serverType = await _context.ServerTypes.FindAsync(id);
            if (serverType == null)
            {
                _logger.LogWarning("Server type not found for update: {Id}", id);
                return NotFound(new { message = "Server type not found" });
            }

            // Check if another server type with same name already exists
            var existingServerType = await _context.ServerTypes
                .FirstOrDefaultAsync(st => st.Name.ToLower() == request.Name.ToLower() && st.Id != id);

            if (existingServerType != null)
            {
                _logger.LogWarning("Server type with name {Name} already exists", request.Name);
                return BadRequest(new { message = "A server type with this name already exists" });
            }

            // Update properties
            serverType.Name = request.Name;
            serverType.GameName = request.GameName;
            serverType.Description = request.Description;
            serverType.CpuCores = request.CpuCores;
            serverType.RamMb = request.RamMb;
            serverType.DiskGb = request.DiskGb;
            serverType.MaxPlayers = request.MaxPlayers;
            serverType.HourlyPrice = request.HourlyPrice;
            serverType.MonthlyPrice = request.MonthlyPrice;
            serverType.DockerImage = request.DockerImage;
            serverType.DefaultConfig = request.DefaultConfig;
            serverType.IsActive = request.IsActive;
            serverType.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dto = new ServerTypeDto
            {
                Id = serverType.Id,
                Name = serverType.Name,
                GameName = serverType.GameName,
                Description = serverType.Description,
                CpuCores = serverType.CpuCores,
                RamMb = serverType.RamMb,
                DiskGb = serverType.DiskGb,
                MaxPlayers = serverType.MaxPlayers,
                HourlyPrice = serverType.HourlyPrice,
                MonthlyPrice = serverType.MonthlyPrice,
                DockerImage = serverType.DockerImage,
                DefaultConfig = serverType.DefaultConfig,
                IsActive = serverType.IsActive,
                CreatedAt = serverType.CreatedAt,
                UpdatedAt = serverType.UpdatedAt
            };

            _logger.LogInformation("Server type updated: {Id} by user {UserId}", id, userId);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating server type {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating server type" });
        }
    }

    /// <summary>
    /// Delete a server type (Admin only)
    /// </summary>
    /// <param name="id">Server type ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteServerType(Guid id)
    {
        try
        {
            // Check if user is admin
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized server type deletion attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role != UserRole.Admin)
            {
                _logger.LogWarning("Non-admin user {UserId} attempted to delete server type", userId);
                return StatusCode(403, new { message = "Only administrators can delete server types" });
            }

            var serverType = await _context.ServerTypes
                .Include(st => st.GameServers)
                .Include(st => st.ServerPlans)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (serverType == null)
            {
                _logger.LogWarning("Server type not found for deletion: {Id}", id);
                return NotFound(new { message = "Server type not found" });
            }

            // Check if server type is in use
            if (serverType.GameServers.Any() || serverType.ServerPlans.Any())
            {
                _logger.LogWarning("Cannot delete server type {Id} - it is in use", id);
                return BadRequest(new { message = "Cannot delete server type that is in use by game servers or server plans" });
            }

            _context.ServerTypes.Remove(serverType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Server type deleted: {Id} by user {UserId}", id, userId);
            return Ok(new { message = "Server type deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting server type {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting server type" });
        }
    }
}

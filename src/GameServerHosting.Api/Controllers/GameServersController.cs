using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// API endpoints for managing game servers
/// </summary>
[ApiController]
[Route("api/servers")]
[Authorize]
public class GameServersController : ControllerBase
{
    private readonly GameServerDbContext _context;
    private readonly IServerManagementService _serverManagement;
    private readonly IServerProvisioningService _provisioningService;
    private readonly ILogger<GameServersController> _logger;

    public GameServersController(
        GameServerDbContext context,
        IServerManagementService serverManagement,
        IServerProvisioningService provisioningService,
        ILogger<GameServersController> logger)
    {
        _context = context;
        _serverManagement = serverManagement;
        _provisioningService = provisioningService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all game servers for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GameServerDto>>> GetServers()
    {
        var userId = GetUserId();

        var servers = await _context.GameServers
            .Where(s => s.UserId == userId)
            .Include(s => s.ServerType)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new GameServerDto
            {
                Id = s.Id,
                ServerName = s.ServerName,
                IpAddress = s.IpAddress,
                Port = s.Port,
                Status = s.Status,
                Version = s.Version,
                DeployedAt = s.DeployedAt,
                ExpiresAt = s.ExpiresAt,
                LastHeartbeatAt = s.LastHeartbeatAt,
                CreatedAt = s.CreatedAt,
                ServerTypeName = s.ServerType.Name,
                GameName = s.ServerType.GameName
            })
            .ToListAsync();

        return Ok(servers);
    }

    /// <summary>
    /// Gets all game servers for a specific user
    /// </summary>
    /// <param name="userId">The ID of the user whose servers to retrieve</param>
    /// <returns>List of game servers for the specified user</returns>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<GameServerDto>>> GetServersByUserId(Guid userId)
    {
        var currentUserId = GetUserId();

        // Check if the current user is requesting their own servers or is an admin
        if (currentUserId != userId)
        {
            // Check if user is an admin
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || currentUser.Role != UserRole.Admin)
            {
                _logger.LogWarning(
                    "User {CurrentUserId} attempted to access servers for user {TargetUserId} without admin privileges",
                    currentUserId, userId);
                return Forbid();
            }
        }

        var servers = await _context.GameServers
            .Where(s => s.UserId == userId)
            .Include(s => s.ServerType)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new GameServerDto
            {
                Id = s.Id,
                ServerName = s.ServerName,
                IpAddress = s.IpAddress,
                Port = s.Port,
                Status = s.Status,
                Version = s.Version,
                DeployedAt = s.DeployedAt,
                ExpiresAt = s.ExpiresAt,
                LastHeartbeatAt = s.LastHeartbeatAt,
                CreatedAt = s.CreatedAt,
                ServerTypeName = s.ServerType.Name,
                GameName = s.ServerType.GameName
            })
            .ToListAsync();

        return Ok(servers);
    }

    /// <summary>
    /// Gets a specific game server by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GameServerDto>> GetServer(Guid id)
    {
        var userId = GetUserId();

        var server = await _context.GameServers
            .Where(s => s.Id == id && s.UserId == userId)
            .Include(s => s.ServerType)
            .Select(s => new GameServerDto
            {
                Id = s.Id,
                ServerName = s.ServerName,
                IpAddress = s.IpAddress,
                Port = s.Port,
                Status = s.Status,
                Version = s.Version,
                DeployedAt = s.DeployedAt,
                ExpiresAt = s.ExpiresAt,
                LastHeartbeatAt = s.LastHeartbeatAt,
                CreatedAt = s.CreatedAt,
                ServerTypeName = s.ServerType.Name,
                GameName = s.ServerType.GameName
            })
            .FirstOrDefaultAsync();

        if (server == null)
        {
            return NotFound(new { message = "Server not found" });
        }

        return Ok(server);
    }

    /// <summary>
    /// Gets the provisioning status of a server
    /// </summary>
    [HttpGet("{id}/status")]
    public async Task<ActionResult<ProvisioningStatus>> GetProvisioningStatus(Guid id)
    {
        var userId = GetUserId();

        // Verify ownership
        var server = await _context.GameServers
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (server == null)
        {
            return NotFound(new { message = "Server not found" });
        }

        try
        {
            var status = await _provisioningService.GetProvisioningStatusAsync(id);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provisioning status for server {ServerId}", id);
            return StatusCode(500, new { message = "Error getting server status" });
        }
    }

    /// <summary>
    /// Starts a stopped or suspended server
    /// </summary>
    [HttpPost("{id}/start")]
    public async Task<ActionResult> StartServer(Guid id)
    {
        var userId = GetUserId();

        // Verify ownership
        var server = await _context.GameServers
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (server == null)
        {
            return NotFound(new { message = "Server not found" });
        }

        try
        {
            var result = await _serverManagement.StartServerAsync(id);
            
            if (result)
            {
                return Ok(new { message = "Server is starting", serverId = id });
            }

            return BadRequest(new { message = "Failed to start server" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting server {ServerId}", id);
            return StatusCode(500, new { message = "Error starting server" });
        }
    }

    /// <summary>
    /// Stops a running server
    /// </summary>
    [HttpPost("{id}/stop")]
    public async Task<ActionResult> StopServer(Guid id)
    {
        var userId = GetUserId();

        // Verify ownership
        var server = await _context.GameServers
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (server == null)
        {
            return NotFound(new { message = "Server not found" });
        }

        try
        {
            var result = await _serverManagement.StopServerAsync(id);
            
            if (result)
            {
                return Ok(new { message = "Server is stopping", serverId = id });
            }

            return BadRequest(new { message = "Failed to stop server" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping server {ServerId}", id);
            return StatusCode(500, new { message = "Error stopping server" });
        }
    }

    /// <summary>
    /// Restarts a running server
    /// </summary>
    [HttpPost("{id}/restart")]
    public async Task<ActionResult> RestartServer(Guid id)
    {
        var userId = GetUserId();

        // Verify ownership
        var server = await _context.GameServers
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (server == null)
        {
            return NotFound(new { message = "Server not found" });
        }

        try
        {
            var result = await _serverManagement.RestartServerAsync(id);
            
            if (result)
            {
                return Ok(new { message = "Server is restarting", serverId = id });
            }

            return BadRequest(new { message = "Failed to restart server" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting server {ServerId}", id);
            return StatusCode(500, new { message = "Error restarting server" });
        }
    }

    /// <summary>
    /// Deletes a server permanently
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteServer(Guid id)
    {
        var userId = GetUserId();

        // Verify ownership
        var server = await _context.GameServers
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (server == null)
        {
            return NotFound(new { message = "Server not found" });
        }

        try
        {
            var result = await _serverManagement.DeleteServerAsync(id);
            
            if (result)
            {
                return Ok(new { message = "Server deleted successfully", serverId = id });
            }

            return BadRequest(new { message = "Failed to delete server" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting server {ServerId}", id);
            return StatusCode(500, new { message = "Error deleting server" });
        }
    }

    /// <summary>
    /// Gets the health status of a server
    /// </summary>
    [HttpGet("{id}/health")]
    public async Task<ActionResult<ServerHealthStatus>> GetServerHealth(Guid id)
    {
        var userId = GetUserId();

        // Verify ownership
        var server = await _context.GameServers
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (server == null)
        {
            return NotFound(new { message = "Server not found" });
        }

        try
        {
            var health = await _serverManagement.CheckServerHealthAsync(id);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for server {ServerId}", id);
            return StatusCode(500, new { message = "Error checking server health" });
        }
    }

    /// <summary>
    /// Gets resource metrics for a server
    /// </summary>
    [HttpGet("{id}/metrics")]
    public async Task<ActionResult<ServerMetrics>> GetServerMetrics(Guid id)
    {
        var userId = GetUserId();

        // Verify ownership
        var server = await _context.GameServers
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (server == null)
        {
            return NotFound(new { message = "Server not found" });
        }

        try
        {
            var metrics = await _serverManagement.GetServerMetricsAsync(id);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for server {ServerId}", id);
            return StatusCode(500, new { message = "Error getting server metrics" });
        }
    }

    #region Helper Methods

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    #endregion
}

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
/// Dashboard data controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly GameServerDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        GameServerDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview data
    /// </summary>
    /// <returns>Dashboard data including user profile, servers, orders, and stats</returns>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userGuid = Guid.Parse(userId);

            // Get user profile
            var userProfile = new UserProfileResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            };

            // Get active servers
            var activeServers = await _context.GameServers
                .Include(s => s.ServerType)
                .Where(s => s.UserId == userGuid && s.Status != ServerStatus.Terminated)
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
                    GameName = s.ServerType.GameName,
                    CpuCores = s.ServerType.CpuCores,
                    RamMb = s.ServerType.RamMb,
                    MaxPlayers = s.ServerType.MaxPlayers
                })
                .ToListAsync();

            // Get recent orders (last 10)
            var recentOrders = await _context.Orders
                .Include(o => o.GameServer)
                    .ThenInclude(s => s!.ServerType)
                .Include(o => o.ServerPlan)
                .Where(o => o.UserId == userGuid)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
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
                    GameName = o.GameServer != null ? o.GameServer.ServerType.GameName : null,
                    PlanName = o.ServerPlan != null ? o.ServerPlan.Name : null
                })
                .ToListAsync();

            // Calculate statistics
            var allServers = await _context.GameServers
                .Where(s => s.UserId == userGuid)
                .ToListAsync();

            var allOrders = await _context.Orders
                .Where(o => o.UserId == userGuid && o.Status == OrderStatus.Completed)
                .ToListAsync();

            var stats = new DashboardStats
            {
                TotalServers = allServers.Count,
                RunningServers = allServers.Count(s => s.Status == ServerStatus.Running),
                TotalOrders = allOrders.Count,
                TotalSpent = allOrders.Sum(o => o.TotalAmount),
                Currency = "USD"
            };

            var dashboard = new DashboardDto
            {
                UserProfile = userProfile,
                ActiveServers = activeServers,
                RecentOrders = recentOrders,
                Stats = stats
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data");
            return StatusCode(500, new { message = "An error occurred while retrieving dashboard data" });
        }
    }

    /// <summary>
    /// Get all user's game servers
    /// </summary>
    /// <returns>List of game servers</returns>
    [HttpGet("servers")]
    [ProducesResponseType(typeof(List<GameServerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<GameServerDto>>> GetServers()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            var servers = await _context.GameServers
                .Include(s => s.ServerType)
                .Where(s => s.UserId == userGuid)
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
                    GameName = s.ServerType.GameName,
                    CpuCores = s.ServerType.CpuCores,
                    RamMb = s.ServerType.RamMb,
                    MaxPlayers = s.ServerType.MaxPlayers
                })
                .ToListAsync();

            return Ok(servers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving game servers");
            return StatusCode(500, new { message = "An error occurred while retrieving servers" });
        }
    }

    /// <summary>
    /// Get all user's orders
    /// </summary>
    /// <returns>List of orders</returns>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<OrderDto>>> GetOrders()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);

            var orders = await _context.Orders
                .Include(o => o.GameServer)
                    .ThenInclude(s => s!.ServerType)
                .Include(o => o.ServerPlan)
                .Where(o => o.UserId == userGuid)
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
                    GameName = o.GameServer != null ? o.GameServer.ServerType.GameName : null,
                    PlanName = o.ServerPlan != null ? o.ServerPlan.Name : null
                })
                .ToListAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }
}

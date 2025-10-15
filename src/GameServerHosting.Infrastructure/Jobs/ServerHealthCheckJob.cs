using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Jobs;

/// <summary>
/// Background job that periodically checks the health of all running servers
/// </summary>
public class ServerHealthCheckJob
{
    private readonly GameServerDbContext _context;
    private readonly IServerManagementService _serverManagement;
    private readonly IServerStatusNotificationService _notificationService;
    private readonly ILogger<ServerHealthCheckJob> _logger;

    public ServerHealthCheckJob(
        GameServerDbContext context,
        IServerManagementService serverManagement,
        IServerStatusNotificationService notificationService,
        ILogger<ServerHealthCheckJob> logger)
    {
        _context = context;
        _serverManagement = serverManagement;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Executes health checks for all running servers
    /// </summary>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting health check job");

        try
        {
            // Get all running servers
            var runningServers = await _context.GameServers
                .Where(s => s.Status == ServerStatus.Running)
                .Include(s => s.User)
                .ToListAsync();

            _logger.LogInformation(
                "Checking health for {Count} running servers",
                runningServers.Count);

            var healthyCount = 0;
            var unhealthyCount = 0;

            foreach (var server in runningServers)
            {
                try
                {
                    var healthStatus = await _serverManagement.CheckServerHealthAsync(server.Id);

                    if (healthStatus.IsHealthy)
                    {
                        healthyCount++;
                    }
                    else
                    {
                        unhealthyCount++;
                        
                        _logger.LogWarning(
                            "Server {ServerId} is unhealthy: {ErrorMessage}",
                            server.Id, healthStatus.ErrorMessage);

                        // Send notification if health changed from healthy to unhealthy
                        if (server.HealthCheckStatus == "healthy")
                        {
                            await _notificationService.SendHealthChangeNotificationAsync(
                                server, false, healthStatus.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error checking health for server {ServerId}",
                        server.Id);
                }
            }

            _logger.LogInformation(
                "Health check completed: {Healthy} healthy, {Unhealthy} unhealthy",
                healthyCount, unhealthyCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing health check job");
        }
    }
}

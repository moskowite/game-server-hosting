using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GameServerHosting.Infrastructure.Services;

/// <summary>
/// Service for managing game server lifecycle and status
/// </summary>
public class ServerManagementService : IServerManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudProviderService _cloudProvider;
    private readonly IServerStatusNotificationService _notificationService;
    private readonly ILogger<ServerManagementService> _logger;

    public ServerManagementService(
        IUnitOfWork unitOfWork,
        ICloudProviderService cloudProvider,
        IServerStatusNotificationService notificationService,
        ILogger<ServerManagementService> logger)
    {
        _unitOfWork = unitOfWork;
        _cloudProvider = cloudProvider;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> StartServerAsync(Guid serverId)
    {
        var server = await _unitOfWork.GameServers.GetByIdAsync(serverId);

        if (server == null)
        {
            _logger.LogWarning("Server {ServerId} not found", serverId);
            return false;
        }

        // Validate current status
        if (server.Status != ServerStatus.Stopped && server.Status != ServerStatus.Suspended)
        {
            _logger.LogWarning(
                "Cannot start server {ServerId} in {Status} state",
                serverId, server.Status);
            throw new InvalidOperationException(
                $"Cannot start server in {server.Status} state. Server must be Stopped or Suspended.");
        }

        if (string.IsNullOrEmpty(server.ProviderId))
        {
            _logger.LogError("Server {ServerId} has no provider ID", serverId);
            return false;
        }

        try
        {
            // Update status to Starting
            await UpdateServerStatusAsync(serverId, ServerStatus.Starting);

            // Start via cloud provider
            var result = await _cloudProvider.StartServerAsync(server.ProviderId);

            if (result)
            {
                // Update to Running
                await UpdateServerStatusAsync(serverId, ServerStatus.Running);

                _logger.LogInformation(
                    "Successfully started server {ServerId}",
                    serverId);

                return true;
            }

            // Revert to previous status on failure
            await UpdateServerStatusAsync(serverId, server.Status, "Failed to start server");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting server {ServerId}", serverId);
            await UpdateServerStatusAsync(serverId, ServerStatus.Failed, ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StopServerAsync(Guid serverId)
    {
        var server = await _unitOfWork.GameServers.GetByIdAsync(serverId);

        if (server == null)
        {
            _logger.LogWarning("Server {ServerId} not found", serverId);
            return false;
        }

        if (server.Status != ServerStatus.Running)
        {
            _logger.LogWarning(
                "Cannot stop server {ServerId} in {Status} state",
                serverId, server.Status);
            throw new InvalidOperationException(
                $"Cannot stop server in {server.Status} state. Server must be Running.");
        }

        if (string.IsNullOrEmpty(server.ProviderId))
        {
            _logger.LogError("Server {ServerId} has no provider ID", serverId);
            return false;
        }

        try
        {
            // Stop via cloud provider
            var result = await _cloudProvider.StopServerAsync(server.ProviderId);

            if (result)
            {
                // Update status
                await UpdateServerStatusAsync(serverId, ServerStatus.Stopped);

                _logger.LogInformation(
                    "Successfully stopped server {ServerId}",
                    serverId);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping server {ServerId}", serverId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RestartServerAsync(Guid serverId)
    {
        var server = await _unitOfWork.GameServers.GetByIdAsync(serverId);

        if (server == null)
        {
            _logger.LogWarning("Server {ServerId} not found", serverId);
            return false;
        }

        if (server.Status != ServerStatus.Running)
        {
            _logger.LogWarning(
                "Cannot restart server {ServerId} in {Status} state",
                serverId, server.Status);
            throw new InvalidOperationException(
                $"Cannot restart server in {server.Status} state. Server must be Running.");
        }

        if (string.IsNullOrEmpty(server.ProviderId))
        {
            _logger.LogError("Server {ServerId} has no provider ID", serverId);
            return false;
        }

        try
        {
            // Update status to Starting
            await UpdateServerStatusAsync(serverId, ServerStatus.Starting);

            // Restart via cloud provider
            var result = await _cloudProvider.RestartServerAsync(server.ProviderId);

            if (result)
            {
                // Wait a bit for restart to complete
                await Task.Delay(2000);

                // Update to Running
                await UpdateServerStatusAsync(serverId, ServerStatus.Running);

                _logger.LogInformation(
                    "Successfully restarted server {ServerId}",
                    serverId);

                return true;
            }

            // Revert to Running on failure
            await UpdateServerStatusAsync(serverId, ServerStatus.Running, "Restart failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting server {ServerId}", serverId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteServerAsync(Guid serverId)
    {
        var server = await _unitOfWork.GameServers.GetByIdAsync(serverId);

        if (server == null)
        {
            _logger.LogWarning("Server {ServerId} not found", serverId);
            return false;
        }

        if (server.Status == ServerStatus.Terminated)
        {
            _logger.LogWarning("Server {ServerId} already terminated", serverId);
            return true;
        }

        try
        {
            // Delete from cloud provider if it exists
            if (!string.IsNullOrEmpty(server.ProviderId))
            {
                await _cloudProvider.DeleteServerAsync(server.ProviderId);
            }

            // Update status to Terminated
            await UpdateServerStatusAsync(serverId, ServerStatus.Terminated);

            _logger.LogInformation(
                "Successfully deleted server {ServerId}",
                serverId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting server {ServerId}", serverId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateServerStatusAsync(Guid serverId, ServerStatus newStatus, string? reason = null)
    {
        var server = await _unitOfWork.GameServers.GetByIdAsync(serverId);

        if (server == null)
        {
            _logger.LogWarning("Server {ServerId} not found", serverId);
            return false;
        }

        var oldStatus = server.Status;

        // Update status
        server.Status = newStatus;
        server.UpdatedAt = DateTime.UtcNow;

        // Update specific fields based on status
        switch (newStatus)
        {
            case ServerStatus.Running:
                server.LastHeartbeatAt = DateTime.UtcNow;
                server.HealthCheckStatus = "healthy";
                break;

            case ServerStatus.Failed:
                server.ProvisioningError = reason;
                break;

            case ServerStatus.Terminated:
                // Keep historical data but mark as terminated
                break;
        }

        _unitOfWork.GameServers.Update(server);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Server {ServerId} status changed from {OldStatus} to {NewStatus}",
            serverId, oldStatus, newStatus);

        // Send notification if status changed
        if (oldStatus != newStatus)
        {
            await _notificationService.SendStatusChangeNotificationAsync(
                server, oldStatus, newStatus, reason);
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<ServerHealthStatus> CheckServerHealthAsync(Guid serverId)
    {
        var server = await _unitOfWork.GameServers.GetByIdAsync(serverId);

        if (server == null)
        {
            throw new InvalidOperationException($"Server {serverId} not found");
        }

        var healthStatus = new ServerHealthStatus
        {
            ServerId = serverId,
            Status = server.Status,
            LastHealthCheckAt = DateTime.UtcNow,
            LastHeartbeatAt = server.LastHeartbeatAt,
            IsHealthy = false,
            IsReachable = false
        };

        // Can only check health of running servers with provider ID
        if (server.Status != ServerStatus.Running || string.IsNullOrEmpty(server.ProviderId))
        {
            healthStatus.ErrorMessage = "Server is not running or has no provider ID";
            return healthStatus;
        }

        try
        {
            // Check cloud provider status
            var providerInfo = await _cloudProvider.GetServerStatusAsync(server.ProviderId);
            healthStatus.ProviderStatus = providerInfo.Status;
            healthStatus.IsReachable = providerInfo.IsHealthy;
            healthStatus.IsHealthy = providerInfo.Status == "active" && providerInfo.IsHealthy;

            // Update database
            server.LastHeartbeatAt = DateTime.UtcNow;
            server.HealthCheckStatus = healthStatus.IsHealthy ? "healthy" : "unhealthy";
            server.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GameServers.Update(server);
        await _unitOfWork.SaveChangesAsync();

            _logger.LogDebug(
                "Health check for server {ServerId}: {IsHealthy}",
                serverId, healthStatus.IsHealthy);

            return healthStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for server {ServerId}", serverId);
            
            healthStatus.IsHealthy = false;
            healthStatus.ErrorMessage = ex.Message;
            
            return healthStatus;
        }
    }

    /// <inheritdoc/>
    public async Task<ServerMetrics> GetServerMetricsAsync(Guid serverId)
    {
        var server = await _unitOfWork.GameServers.GetByIdAsync(serverId);

        if (server == null)
        {
            throw new InvalidOperationException($"Server {serverId} not found");
        }

        if (string.IsNullOrEmpty(server.ProviderId))
        {
            return new ServerMetrics { Timestamp = DateTime.UtcNow };
        }

        try
        {
            var metrics = await _cloudProvider.GetServerMetricsAsync(server.ProviderId);
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for server {ServerId}", serverId);
            return new ServerMetrics { Timestamp = DateTime.UtcNow };
        }
    }
}

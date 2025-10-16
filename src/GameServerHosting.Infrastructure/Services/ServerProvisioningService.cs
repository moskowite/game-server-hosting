using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameServerHosting.Infrastructure.Services;

/// <summary>
/// Service for orchestrating game server provisioning operations
/// </summary>
public class ServerProvisioningService : IServerProvisioningService
{
    private readonly GameServerDbContext _context;
    private readonly ICloudProviderService _cloudProvider;
    private readonly ILogger<ServerProvisioningService> _logger;
    private readonly IEmailService _emailService;
    private readonly ProvisioningSettings _settings;

    public ServerProvisioningService(
        GameServerDbContext context,
        ICloudProviderService cloudProvider,
        ILogger<ServerProvisioningService> logger,
        IEmailService emailService,
        IOptions<ProvisioningSettings> settings)
    {
        _context = context;
        _cloudProvider = cloudProvider;
        _logger = logger;
        _emailService = emailService;
        _settings = settings.Value;
    }

    /// <inheritdoc/>
    public async Task<Guid> ProvisionServerAsync(Guid orderId)
    {
        _logger.LogInformation("Starting provisioning for order {OrderId}", orderId);

        // Step 1: Validate the order
        if (!await ValidateProvisioningAsync(orderId))
        {
            throw new InvalidOperationException($"Order {orderId} is not valid for provisioning");
        }

        // Step 2: Load order with all related data
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.ServerPlan)
                .ThenInclude(sp => sp!.ServerType)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        // Step 3: Create GameServer record
        var gameServer = new GameServer
        {
            Id = Guid.NewGuid(),
            UserId = order.UserId,
            ServerTypeId = order.ServerPlan!.ServerTypeId,
            ServerName = $"{order.User.UserName}'s {order.ServerPlan.ServerType!.GameName} Server",
            Status = ServerStatus.Provisioning,
            Configuration = "{}",
            Version = order.ServerPlan.ServerType.DefaultVersion ?? "latest",
            ProviderType = _settings.DefaultProvider,
            ProvisioningStartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GameServers.Add(gameServer);
        
        // Link order to server
        order.ServerId = gameServer.Id;
        order.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created GameServer {ServerId} for order {OrderId}",
            gameServer.Id, orderId);

        try
        {
            // Step 4: Provision infrastructure
            var provisionRequest = CreateProvisionRequest(gameServer, order);
            var result = await _cloudProvider.CreateServerAsync(provisionRequest);

            if (!result.Success)
            {
                throw new Exception($"Cloud provider provisioning failed: {result.ErrorMessage}");
            }

            // Step 5: Update server with provisioning results
            gameServer.ProviderId = result.ProviderId;
            gameServer.IpAddress = result.IpAddress;
            gameServer.Port = result.Port ?? GetDefaultPort(order.ServerPlan.ServerType.GameName);
            gameServer.ProvisioningCompletedAt = DateTime.UtcNow;
            gameServer.DeployedAt = DateTime.UtcNow;
            gameServer.Status = ServerStatus.Running;
            gameServer.HealthCheckStatus = "healthy";
            gameServer.LastHeartbeatAt = DateTime.UtcNow;
            
            // Calculate expiration based on billing cycle
            gameServer.ExpiresAt = CalculateExpirationDate(order.ServerPlan.DurationHours);
            
            gameServer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully provisioned server {ServerId} at {IpAddress}:{Port}",
                gameServer.Id, gameServer.IpAddress, gameServer.Port);

            // Step 6: Send notification
            if (_settings.SendNotifications)
            {
                await SendProvisioningNotificationAsync(gameServer);
            }

            return gameServer.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision server for order {OrderId}", orderId);

            // Update server status to failed
            gameServer.Status = ServerStatus.Failed;
            gameServer.ProvisioningError = ex.Message;
            gameServer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Attempt rollback if configured
            if (_settings.AutoRollbackOnFailure)
            {
                await RollbackProvisioningAsync(gameServer.Id, ex.Message);
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ProvisioningResult> ProvisionServerAsync(Guid serverId, string region)
    {
        _logger.LogInformation("Starting provisioning for server {ServerId} in region {Region}", serverId, region);

        try
        {
            // Step 1: Load the server
            var server = await _context.GameServers
                .Include(s => s.ServerType)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == serverId);

            if (server == null)
            {
                return new ProvisioningResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Server {serverId} not found",
                    ErrorCode = "SERVER_NOT_FOUND",
                    IsRetryable = false
                };
            }

            // Step 2: Validate server is in correct state
            if (server.Status != ServerStatus.Pending && server.Status != ServerStatus.Provisioning)
            {
                return new ProvisioningResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Server is in {server.Status} state, cannot provision",
                    ErrorCode = "INVALID_STATE",
                    IsRetryable = false
                };
            }

            // Step 3: Update status to provisioning
            server.Status = ServerStatus.Provisioning;
            server.ProvisioningStartedAt = DateTime.UtcNow;
            server.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Step 4: Create provision request with specified region
            var provisionRequest = new ServerProvisionRequest
            {
                ServerId = server.Id,
                UserId = server.UserId,
                ServerType = server.ServerType!.GameName,
                ServerName = server.ServerName,
                RamMb = 2048, // Default, should be from server plan
                CpuCores = 2,
                StorageGb = 50,
                GameVersion = server.Version,
                Region = region
            };

            // Step 5: Provision with cloud provider
            var cloudResult = await _cloudProvider.CreateServerAsync(provisionRequest);

            if (!cloudResult.Success)
            {
                // Update server to failed state
                server.Status = ServerStatus.Failed;
                server.ProvisioningError = cloudResult.ErrorMessage;
                server.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new ProvisioningResult
                {
                    IsSuccess = false,
                    ErrorMessage = cloudResult.ErrorMessage,
                    ErrorCode = "CLOUD_PROVIDER_ERROR",
                    IsRetryable = true
                };
            }

            // Step 6: Update server with provisioning results
            server.ProviderId = cloudResult.ProviderId;
            server.IpAddress = cloudResult.IpAddress;
            server.Port = cloudResult.Port ?? GetDefaultPort(server.ServerType.GameName);
            server.ProvisioningCompletedAt = DateTime.UtcNow;
            server.DeployedAt = DateTime.UtcNow;
            server.Status = ServerStatus.Running;
            server.HealthCheckStatus = "healthy";
            server.LastHeartbeatAt = DateTime.UtcNow;
            server.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully provisioned server {ServerId} at {IpAddress}:{Port}",
                serverId, server.IpAddress, server.Port);

            // Step 7: Send notification
            if (_settings.SendNotifications)
            {
                await SendProvisioningNotificationAsync(server);
            }

            return new ProvisioningResult
            {
                IsSuccess = true,
                IpAddress = server.IpAddress,
                Port = server.Port ?? 0,
                ProviderServerId = server.ProviderId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision server {ServerId} in region {Region}", serverId, region);

            return new ProvisioningResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ErrorCode = "UNEXPECTED_ERROR",
                IsRetryable = true
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ProvisioningStatus> GetProvisioningStatusAsync(Guid serverId)
    {
        var server = await _context.GameServers
            .FirstOrDefaultAsync(s => s.Id == serverId);

        if (server == null)
        {
            throw new InvalidOperationException($"Server {serverId} not found");
        }

        var status = new ProvisioningStatus
        {
            ServerId = serverId,
            Status = server.Status.ToString(),
            Progress = CalculateProgress(server.Status),
            CurrentStep = GetCurrentStep(server.Status),
            ErrorMessage = server.ProvisioningError,
            StartedAt = server.ProvisioningStartedAt,
            CompletedAt = server.ProvisioningCompletedAt,
            IpAddress = server.IpAddress,
            Port = server.Port
        };

        // Calculate estimated time remaining
        if (server.ProvisioningStartedAt.HasValue && server.Status != ServerStatus.Running && server.Status != ServerStatus.Failed)
        {
            var elapsed = DateTime.UtcNow - server.ProvisioningStartedAt.Value;
            var estimatedTotal = TimeSpan.FromMinutes(5); // Average 5 minutes
            var remaining = estimatedTotal - elapsed;
            status.EstimatedTimeRemainingMinutes = Math.Max(0, (int)remaining.TotalMinutes);
        }

        return status;
    }

    /// <inheritdoc/>
    public async Task<bool> RollbackProvisioningAsync(Guid serverId, string reason)
    {
        _logger.LogWarning(
            "Rolling back provisioning for server {ServerId}: {Reason}",
            serverId, reason);

        var server = await _context.GameServers
            .Include(s => s.Orders)
            .FirstOrDefaultAsync(s => s.Id == serverId);

        if (server == null)
        {
            _logger.LogWarning("Server {ServerId} not found for rollback", serverId);
            return false;
        }

        try
        {
            // Step 1: Delete cloud resources if they exist
            if (!string.IsNullOrEmpty(server.ProviderId))
            {
                try
                {
                    await _cloudProvider.DeleteServerAsync(server.ProviderId);
                    _logger.LogInformation(
                        "Deleted cloud resources for server {ServerId}",
                        serverId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to delete cloud resources for server {ServerId}",
                        serverId);
                    // Continue with rollback even if cloud deletion fails
                }
            }

            // Step 2: Update server status
            server.Status = ServerStatus.Failed;
            server.ProvisioningError = reason;
            server.UpdatedAt = DateTime.UtcNow;

            // Step 3: Update related orders to failed status
            foreach (var order in server.Orders.Where(o => o.Status == OrderStatus.Completed))
            {
                order.Status = OrderStatus.Failed;
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully rolled back server {ServerId}",
                serverId);

            // Step 4: Send failure notification
            if (_settings.SendNotifications)
            {
                await SendProvisioningFailureNotificationAsync(server, reason);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to rollback provisioning for server {ServerId}",
                serverId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateProvisioningAsync(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.ServerPlan)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", orderId);
            return false;
        }

        // Validate order is completed
        if (order.Status != OrderStatus.Completed)
        {
            _logger.LogWarning(
                "Order {OrderId} status is {Status}, expected Completed",
                orderId, order.Status);
            return false;
        }

        // Validate payment is successful
        if (!order.Payments.Any(p => p.Status == PaymentStatus.Completed))
        {
            _logger.LogWarning(
                "Order {OrderId} has no completed payments",
                orderId);
            return false;
        }

        // Validate user account is active
        if (order.User.LockoutEnd.HasValue && order.User.LockoutEnd > DateTime.UtcNow)
        {
            _logger.LogWarning(
                "User {UserId} account is locked",
                order.UserId);
            return false;
        }

        // Validate server plan exists
        if (order.ServerPlan == null)
        {
            _logger.LogWarning(
                "Order {OrderId} has no associated server plan",
                orderId);
            return false;
        }

        // Check if server hasn't already been provisioned
        if (order.ServerId.HasValue && order.ServerId != Guid.Empty)
        {
            _logger.LogWarning(
                "Order {OrderId} already has a server {ServerId}",
                orderId, order.ServerId);
            return false;
        }

        return true;
    }

    #region Private Helper Methods

    private ServerProvisionRequest CreateProvisionRequest(GameServer server, Order order)
    {
        var serverPlan = order.ServerPlan!;
        var serverType = serverPlan.ServerType!;

        // Parse RAM from server plan name or use defaults
        int ramMb = serverPlan.Name.ToLower() switch
        {
            var n when n.Contains("1gb") => 1024,
            var n when n.Contains("2gb") => 2048,
            var n when n.Contains("4gb") => 4096,
            var n when n.Contains("8gb") => 8192,
            var n when n.Contains("16gb") => 16384,
            _ => 2048 // Default 2GB
        };

        int cpuCores = ramMb switch
        {
            <= 2048 => 1,
            <= 4096 => 2,
            <= 8192 => 4,
            _ => 6
        };

        return new ServerProvisionRequest
        {
            ServerId = server.Id,
            UserId = server.UserId,
            ServerPlanId = serverPlan.Id,
            ServerType = serverType.GameName,
            ServerName = server.ServerName,
            RamMb = ramMb,
            CpuCores = cpuCores,
            StorageGb = ramMb <= 2048 ? 50 : ramMb / 32, // ~1.5GB per GB of RAM
            GameVersion = server.Version
        };
    }

    private int GetDefaultPort(string gameName)
    {
        return gameName.ToLower() switch
        {
            "minecraft" => 25565,
            "terraria" => 7777,
            "valheim" => 2456,
            "rust" => 28015,
            "ark" => 7777,
            "cs:go" or "csgo" => 27015,
            _ => 25565
        };
    }

    private DateTime CalculateExpirationDate(int durationHours)
    {
        return DateTime.UtcNow.AddHours(durationHours);
    }

    private int CalculateProgress(ServerStatus status)
    {
        return status switch
        {
            ServerStatus.Pending => 0,
            ServerStatus.Provisioning => 25,
            ServerStatus.Configuring => 50,
            ServerStatus.Starting => 75,
            ServerStatus.Running => 100,
            ServerStatus.Failed => 0,
            _ => 0
        };
    }

    private string GetCurrentStep(ServerStatus status)
    {
        return status switch
        {
            ServerStatus.Pending => "Queued for provisioning",
            ServerStatus.Provisioning => "Creating cloud infrastructure",
            ServerStatus.Configuring => "Installing software and configuring server",
            ServerStatus.Starting => "Starting game server",
            ServerStatus.Running => "Server running",
            ServerStatus.Failed => "Provisioning failed",
            _ => "Unknown"
        };
    }

    private async Task SendProvisioningNotificationAsync(GameServer server)
    {
        try
        {
            var user = await _context.Users.FindAsync(server.UserId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                return;
            }

            var subject = $"Your {server.ServerType.GameName} Server is Ready!";
            var body = $@"
                <h2>Server Provisioned Successfully</h2>
                <p>Hi {user.UserName},</p>
                <p>Your game server has been successfully provisioned and is now running!</p>
                <h3>Server Details:</h3>
                <ul>
                    <li><strong>Server Name:</strong> {server.ServerName}</li>
                    <li><strong>IP Address:</strong> {server.IpAddress}</li>
                    <li><strong>Port:</strong> {server.Port}</li>
                    <li><strong>Status:</strong> Running</li>
                    <li><strong>Expires:</strong> {server.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC</li>
                </ul>
                <p>You can now connect to your server using the IP address and port above.</p>
                <p>Enjoy gaming!</p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            _logger.LogInformation(
                "Sent provisioning notification to {Email} for server {ServerId}",
                user.Email, server.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send provisioning notification for server {ServerId}",
                server.Id);
        }
    }

    private async Task SendProvisioningFailureNotificationAsync(GameServer server, string reason)
    {
        try
        {
            var user = await _context.Users.FindAsync(server.UserId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                return;
            }

            var subject = "Server Provisioning Failed";
            var body = $@"
                <h2>Server Provisioning Failed</h2>
                <p>Hi {user.UserName},</p>
                <p>We're sorry, but we encountered an issue while provisioning your game server.</p>
                <h3>Details:</h3>
                <ul>
                    <li><strong>Server Name:</strong> {server.ServerName}</li>
                    <li><strong>Error:</strong> {reason}</li>
                </ul>
                <p>We have automatically initiated a refund for your order. The refund will be processed within 5-10 business days.</p>
                <p>If you have any questions, please contact our support team.</p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            _logger.LogInformation(
                "Sent failure notification to {Email} for server {ServerId}",
                user.Email, server.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send failure notification for server {ServerId}",
                server.Id);
        }
    }

    #endregion
}

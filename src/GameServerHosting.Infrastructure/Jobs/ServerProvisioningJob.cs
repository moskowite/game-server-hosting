using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Jobs;

/// <summary>
/// Background job for provisioning game servers
/// </summary>
public class ServerProvisioningJob
{
    private readonly IServerProvisioningService _provisioningService;
    private readonly ILogger<ServerProvisioningJob> _logger;
    private readonly GameServerDbContext _context;

    public ServerProvisioningJob(
        IServerProvisioningService provisioningService,
        ILogger<ServerProvisioningJob> logger,
        GameServerDbContext context)
    {
        _provisioningService = provisioningService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Executes the provisioning job for a specific order
    /// </summary>
    /// <param name="orderId">The order ID to provision</param>
    public async Task ExecuteAsync(Guid orderId)
    {
        _logger.LogInformation("Starting provisioning for order {OrderId}", orderId);

        try
        {
            // Validate the order before provisioning
            if (!await _provisioningService.ValidateProvisioningAsync(orderId))
            {
                _logger.LogWarning(
                    "Order {OrderId} is not valid for provisioning",
                    orderId);
                return;
            }

            // Provision the server
            var serverId = await _provisioningService.ProvisionServerAsync(orderId);

            _logger.LogInformation(
                "Successfully provisioned server {ServerId} for order {OrderId}",
                serverId, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to provision server for order {OrderId}",
                orderId);

            // The provisioning service will handle rollback
            throw;
        }
    }
}

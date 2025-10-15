using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Enums;
using GameServerHosting.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GameServerHosting.Infrastructure.Consumers;

/// <summary>
/// Consumer for ServerProvisioningRequested events
/// Handles the actual provisioning of game servers with cloud providers
/// </summary>
public class ServerProvisioningRequestedConsumer : IConsumer<ServerProvisioningRequested>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServerProvisioningService _provisioningService;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<ServerProvisioningRequestedConsumer> _logger;

    public ServerProvisioningRequestedConsumer(
        IUnitOfWork unitOfWork,
        IServerProvisioningService provisioningService,
        IOutboxService outboxService,
        ILogger<ServerProvisioningRequestedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _provisioningService = provisioningService;
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ServerProvisioningRequested> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Processing ServerProvisioningRequested for GameServer {GameServerId}, Order {OrderId}, Correlation {CorrelationId}",
            message.GameServerId, message.OrderId, message.CorrelationId);

        try
        {
            // Update async operation status
            var operation = await _unitOfWork.AsyncOperations.GetByCorrelationIdAsync(message.CorrelationId);
            if (operation != null)
            {
                operation.Status = AsyncOperationStatus.InProgress;
                operation.StatusMessage = "Provisioning server with cloud provider...";
                operation.ProgressPercentage = 25;
                operation.UpdatedAt = DateTime.UtcNow;
            }

            // Get the game server entity
            var gameServer = await _unitOfWork.GameServers.GetByIdAsync(message.GameServerId);
            if (gameServer == null)
            {
                _logger.LogError("GameServer {GameServerId} not found", message.GameServerId);
                throw new InvalidOperationException($"GameServer {message.GameServerId} not found");
            }

            // Update server status to provisioning
            gameServer.Status = ServerStatus.Provisioning;
            gameServer.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            // Provision the server with the cloud provider
            _logger.LogInformation("Starting provisioning for GameServer {GameServerId}", message.GameServerId);
            
            var provisioningResult = await _provisioningService.ProvisionServerAsync(
                message.GameServerId,
                message.Region);

            if (provisioningResult.IsSuccess)
            {
                // Update game server with provisioning details
                gameServer.Status = ServerStatus.Running;
                gameServer.IpAddress = provisioningResult.IpAddress;
                gameServer.Port = provisioningResult.Port;
                gameServer.DeployedAt = DateTime.UtcNow;
                gameServer.UpdatedAt = DateTime.UtcNow;

                // Update async operation
                if (operation != null)
                {
                    operation.Status = AsyncOperationStatus.Succeeded;
                    operation.StatusMessage = "Server provisioned successfully";
                    operation.ProgressPercentage = 100;
                    operation.CompletedAt = DateTime.UtcNow;
                    operation.UpdatedAt = DateTime.UtcNow;
                }

                // Enqueue success event
                await _outboxService.EnqueueAsync(new ServerProvisioned
                {
                    CorrelationId = message.CorrelationId,
                    CausationId = message.EventId,
                    UserId = message.UserId,
                    GameServerId = message.GameServerId,
                    OrderId = message.OrderId,
                    IpAddress = provisioningResult.IpAddress!,
                    Port = provisioningResult.Port,
                    ProviderServerId = provisioningResult.ProviderServerId,
                    Region = message.Region,
                    DeployedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully provisioned GameServer {GameServerId} at {IpAddress}:{Port}",
                    message.GameServerId, provisioningResult.IpAddress, provisioningResult.Port);
            }
            else
            {
                // Provisioning failed
                gameServer.Status = ServerStatus.Failed;
                gameServer.UpdatedAt = DateTime.UtcNow;

                // Update async operation
                if (operation != null)
                {
                    operation.Status = AsyncOperationStatus.Failed;
                    operation.StatusMessage = "Server provisioning failed";
                    operation.ErrorMessage = provisioningResult.ErrorMessage;
                    operation.ProgressPercentage = 0;
                    operation.CompletedAt = DateTime.UtcNow;
                    operation.UpdatedAt = DateTime.UtcNow;
                }

                // Enqueue failure event
                await _outboxService.EnqueueAsync(new ServerProvisioningFailed
                {
                    CorrelationId = message.CorrelationId,
                    CausationId = message.EventId,
                    UserId = message.UserId,
                    GameServerId = message.GameServerId,
                    OrderId = message.OrderId,
                    ErrorMessage = provisioningResult.ErrorMessage ?? "Unknown error",
                    ErrorCode = provisioningResult.ErrorCode,
                    RetryAttempts = 0,
                    IsRetryable = provisioningResult.IsRetryable
                });

                await _unitOfWork.SaveChangesAsync();

                _logger.LogError(
                    "Failed to provision GameServer {GameServerId}: {ErrorMessage}",
                    message.GameServerId, provisioningResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ServerProvisioningRequested for GameServer {GameServerId}", message.GameServerId);

            // Update async operation to failed
            var operation = await _unitOfWork.AsyncOperations.GetByCorrelationIdAsync(message.CorrelationId);
            if (operation != null)
            {
                operation.Status = AsyncOperationStatus.Failed;
                operation.StatusMessage = "Server provisioning encountered an error";
                operation.ErrorMessage = ex.Message;
                operation.CompletedAt = DateTime.UtcNow;
                operation.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }

            // Rethrow to trigger retry mechanism
            throw;
        }
    }
}

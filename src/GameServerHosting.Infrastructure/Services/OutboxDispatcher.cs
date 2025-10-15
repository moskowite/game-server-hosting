using System.Text.Json;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Shared.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServerHosting.Infrastructure.Services;

/// <summary>
/// Background service that periodically processes pending outbox messages and publishes them to MassTransit
/// </summary>
public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5); // Process every 5 seconds

    public OutboxDispatcher(IServiceProvider serviceProvider, ILogger<OutboxDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Dispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Outbox Dispatcher stopped");
    }

    private async Task ProcessPendingMessages(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pendingMessages = await outboxService.GetPendingMessagesAsync(100, cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return; // No messages to process
        }

        _logger.LogDebug("Processing {Count} pending outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            try
            {
                // Deserialize the event
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    _logger.LogError("Unknown event type: {EventType}", message.EventType);
                    await outboxService.MarkAsFailedAsync(message.Id, $"Unknown event type: {message.EventType}", cancellationToken);
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, eventType, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (@event == null)
                {
                    _logger.LogError("Failed to deserialize event {MessageId} of type {EventType}", message.Id, message.EventType);
                    await outboxService.MarkAsFailedAsync(message.Id, "Failed to deserialize event", cancellationToken);
                    continue;
                }

                // Publish the event to MassTransit
                await publishEndpoint.Publish(@event, cancellationToken);

                _logger.LogInformation(
                    "Published outbox message {MessageId} of type {EventType}",
                    message.Id, message.EventType);

                // Mark as processed
                await outboxService.MarkAsProcessedAsync(message.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing outbox message {MessageId}", message.Id);
                await outboxService.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }
}

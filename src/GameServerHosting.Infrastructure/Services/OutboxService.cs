using System.Text.Json;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameServerHosting.Infrastructure.Services;

/// <summary>
/// Implementation of the outbox service for transactional event publishing
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly GameServerDbContext _context;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(GameServerDbContext context, ILogger<OutboxService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task EnqueueAsync<T>(T @event, CancellationToken cancellationToken = default) where T : BaseEvent
    {
        var eventType = @event.GetType().FullName ?? @event.GetType().Name;
        var payload = JsonSerializer.Serialize(@event, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            CorrelationId = @event.CorrelationId
        };

        _context.OutboxMessages.Add(outboxMessage);
        
        _logger.LogDebug(
            "Enqueued outbox message {MessageId} of type {EventType} with correlation {CorrelationId}",
            outboxMessage.Id, eventType, @event.CorrelationId);

        // Note: SaveChanges is called by the UnitOfWork/controller after the entire transaction
    }

    /// <inheritdoc/>
    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .Where(m => m.ProcessingAttempts < 5) // Max retry attempts
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            message.ProcessingAttempts++;
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Marked outbox message {MessageId} as processed", messageId);
        }
    }

    /// <inheritdoc/>
    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.Error = error;
            message.ProcessingAttempts++;
            
            // If max attempts reached, mark as processed to prevent infinite retries
            if (message.ProcessingAttempts >= 5)
            {
                message.ProcessedAt = DateTime.UtcNow;
                _logger.LogError(
                    "Outbox message {MessageId} failed after {Attempts} attempts: {Error}",
                    messageId, message.ProcessingAttempts, error);
            }
            else
            {
                _logger.LogWarning(
                    "Outbox message {MessageId} failed (attempt {Attempts}/5): {Error}",
                    messageId, message.ProcessingAttempts, error);
            }
            
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

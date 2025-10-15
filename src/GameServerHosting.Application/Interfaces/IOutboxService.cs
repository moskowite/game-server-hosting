using GameServerHosting.Shared.Events;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Service for enqueueing domain events to the transactional outbox
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Enqueues a domain event to be published after the current transaction commits
    /// </summary>
    /// <typeparam name="T">Type of the event</typeparam>
    /// <param name="event">The event to enqueue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnqueueAsync<T>(T @event, CancellationToken cancellationToken = default) where T : BaseEvent;

    /// <summary>
    /// Retrieves pending outbox messages that need to be published
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending outbox messages</returns>
    Task<List<Domain.Entities.OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox message as processed
    /// </summary>
    /// <param name="messageId">ID of the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox message as failed
    /// </summary>
    /// <param name="messageId">ID of the message</param>
    /// <param name="error">Error message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}

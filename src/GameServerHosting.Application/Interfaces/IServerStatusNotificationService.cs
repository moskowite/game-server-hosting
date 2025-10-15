using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Service interface for sending server status change notifications
/// </summary>
public interface IServerStatusNotificationService
{
    /// <summary>
    /// Sends a notification when server status changes
    /// </summary>
    /// <param name="server">The game server</param>
    /// <param name="oldStatus">The previous status</param>
    /// <param name="newStatus">The new status</param>
    /// <param name="reason">Optional reason for the change</param>
    Task SendStatusChangeNotificationAsync(
        GameServer server,
        ServerStatus oldStatus,
        ServerStatus newStatus,
        string? reason = null);

    /// <summary>
    /// Sends a notification when server health changes
    /// </summary>
    /// <param name="server">The game server</param>
    /// <param name="isHealthy">Whether the server is healthy</param>
    /// <param name="errorMessage">Optional error message if unhealthy</param>
    Task SendHealthChangeNotificationAsync(
        GameServer server,
        bool isHealthy,
        string? errorMessage = null);
}

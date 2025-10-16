using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameServerHosting.Infrastructure.Services;

/// <summary>
/// Service for sending server status change notifications
/// </summary>
public class ServerStatusNotificationService : IServerStatusNotificationService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ServerStatusNotificationService> _logger;
    private readonly GameServerDbContext _context;

    public ServerStatusNotificationService(
        IEmailService emailService,
        ILogger<ServerStatusNotificationService> logger,
        GameServerDbContext context)
    {
        _emailService = emailService;
        _logger = logger;
        _context = context;
    }

    /// <inheritdoc/>
    public async Task SendStatusChangeNotificationAsync(
        GameServer server,
        ServerStatus oldStatus,
        ServerStatus newStatus,
        string? reason = null)
    {
        try
        {
            // Load user if not already loaded
            if (server.User == null)
            {
                server.User = (await _context.Users.FindAsync(server.UserId))!;
            }

            if (server.User == null || string.IsNullOrEmpty(server.User.Email))
            {
                _logger.LogWarning(
                    "Cannot send notification for server {ServerId}: User or email not found",
                    server.Id);
                return;
            }

            var subject = GetStatusChangeSubject(newStatus);
            var body = GetStatusChangeBody(server, oldStatus, newStatus, reason);

            await _emailService.SendEmailAsync(server.User.Email, subject, body);

            _logger.LogInformation(
                "Sent status change notification to {Email} for server {ServerId}: {OldStatus} -> {NewStatus}",
                server.User.Email, server.Id, oldStatus, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send status change notification for server {ServerId}",
                server.Id);
        }
    }

    /// <inheritdoc/>
    public async Task SendHealthChangeNotificationAsync(
        GameServer server,
        bool isHealthy,
        string? errorMessage = null)
    {
        try
        {
            // Load user if not already loaded
            if (server.User == null)
            {
                server.User = (await _context.Users.FindAsync(server.UserId))!;
            }

            if (server.User == null || string.IsNullOrEmpty(server.User.Email))
            {
                return;
            }

            // Only send notifications for unhealthy servers
            if (!isHealthy)
            {
                var subject = $"Health Alert: {server.ServerName}";
                var body = $@"
                    <h2>Server Health Alert</h2>
                    <p>Hi {server.User.UserName},</p>
                    <p>We detected a health issue with your game server.</p>
                    <h3>Server Details:</h3>
                    <ul>
                        <li><strong>Server Name:</strong> {server.ServerName}</li>
                        <li><strong>IP Address:</strong> {server.IpAddress}</li>
                        <li><strong>Port:</strong> {server.Port}</li>
                        <li><strong>Status:</strong> {server.Status}</li>
                        <li><strong>Issue:</strong> {errorMessage ?? "Server is not responding"}</li>
                    </ul>
                    <p>Our team is monitoring the situation. If the issue persists, please contact support.</p>
                ";

                await _emailService.SendEmailAsync(server.User.Email, subject, body);

                _logger.LogInformation(
                    "Sent health alert notification to {Email} for server {ServerId}",
                    server.User.Email, server.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send health notification for server {ServerId}",
                server.Id);
        }
    }

    #region Private Helper Methods

    private string GetStatusChangeSubject(ServerStatus newStatus)
    {
        return newStatus switch
        {
            ServerStatus.Running => "Your Server is Now Running",
            ServerStatus.Stopped => "Your Server Has Been Stopped",
            ServerStatus.Suspended => "Your Server Has Been Suspended",
            ServerStatus.Failed => "Server Status Alert: Failed",
            ServerStatus.Terminated => "Your Server Has Been Terminated",
            _ => "Server Status Update"
        };
    }

    private string GetStatusChangeBody(
        GameServer server,
        ServerStatus oldStatus,
        ServerStatus newStatus,
        string? reason)
    {
        var statusMessage = GetStatusMessage(newStatus, reason);
        var actionMessage = GetActionMessage(newStatus);

        return $@"
            <h2>Server Status Update</h2>
            <p>Hi {server.User.UserName},</p>
            <p>{statusMessage}</p>
            <h3>Server Details:</h3>
            <ul>
                <li><strong>Server Name:</strong> {server.ServerName}</li>
                <li><strong>Previous Status:</strong> {oldStatus}</li>
                <li><strong>New Status:</strong> {newStatus}</li>
                {(server.IpAddress != null ? $"<li><strong>IP Address:</strong> {server.IpAddress}</li>" : "")}
                {(server.Port.HasValue ? $"<li><strong>Port:</strong> {server.Port}</li>" : "")}
                {(!string.IsNullOrEmpty(reason) ? $"<li><strong>Reason:</strong> {reason}</li>" : "")}
            </ul>
            {actionMessage}
        ";
    }

    private string GetStatusMessage(ServerStatus status, string? reason)
    {
        return status switch
        {
            ServerStatus.Running => "Your game server is now running and ready to accept connections.",
            ServerStatus.Stopped => "Your game server has been stopped.",
            ServerStatus.Suspended => "Your game server has been suspended. " + (reason ?? "Please check your billing information."),
            ServerStatus.Failed => "We encountered an issue with your game server. " + (reason ?? "Our team is investigating."),
            ServerStatus.Terminated => "Your game server has been permanently deleted.",
            ServerStatus.Starting => "Your game server is starting up.",
            ServerStatus.Provisioning => "Your game server is being provisioned.",
            ServerStatus.Configuring => "Your game server is being configured.",
            _ => "Your game server status has been updated."
        };
    }

    private string GetActionMessage(ServerStatus status)
    {
        return status switch
        {
            ServerStatus.Running => "<p>You can now connect to your server using the IP address and port above.</p>",
            ServerStatus.Stopped => "<p>You can restart your server from the dashboard at any time.</p>",
            ServerStatus.Suspended => "<p>Please update your billing information to reactivate your server.</p>",
            ServerStatus.Failed => "<p>Please contact our support team if you need assistance.</p>",
            ServerStatus.Terminated => "<p>All server data has been permanently deleted.</p>",
            _ => ""
        };
    }

    #endregion
}

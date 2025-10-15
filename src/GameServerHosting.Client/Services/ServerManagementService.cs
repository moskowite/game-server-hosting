using System.Net.Http.Json;
using GameServerHosting.Client.Models;

namespace GameServerHosting.Client.Services;

/// <summary>
/// Service for managing game server operations
/// </summary>
public class ServerManagementService
{
    private readonly HttpClient _httpClient;

    public ServerManagementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Starts a stopped or suspended server
    /// </summary>
    public async Task<bool> StartServerAsync(Guid serverId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/servers/{serverId}/start", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting server: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stops a running server
    /// </summary>
    public async Task<bool> StopServerAsync(Guid serverId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/servers/{serverId}/stop", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping server: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Restarts a running server
    /// </summary>
    public async Task<bool> RestartServerAsync(Guid serverId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/servers/{serverId}/restart", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error restarting server: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deletes a server permanently
    /// </summary>
    public async Task<bool> DeleteServerAsync(Guid serverId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/servers/{serverId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting server: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets server metrics (CPU, RAM, disk usage)
    /// </summary>
    public async Task<ServerMetrics?> GetServerMetricsAsync(Guid serverId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ServerMetrics>($"api/servers/{serverId}/metrics");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching server metrics: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets server health status
    /// </summary>
    public async Task<ServerHealthStatus?> GetServerHealthAsync(Guid serverId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ServerHealthStatus>($"api/servers/{serverId}/health");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching server health: {ex.Message}");
            return null;
        }
    }
}

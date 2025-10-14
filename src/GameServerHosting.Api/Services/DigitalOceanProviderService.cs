using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using GameServerHosting.Api.Models;
using Microsoft.Extensions.Options;

namespace GameServerHosting.Api.Services;

/// <summary>
/// DigitalOcean cloud provider implementation
/// </summary>
public class DigitalOceanProviderService : ICloudProviderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DigitalOceanProviderService> _logger;
    private readonly DigitalOceanSettings _settings;

    public DigitalOceanProviderService(
        HttpClient httpClient,
        ILogger<DigitalOceanProviderService> logger,
        IOptions<DigitalOceanSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_settings.ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <inheritdoc/>
    public async Task<ProvisionResult> CreateServerAsync(ServerProvisionRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Creating DigitalOcean droplet for server {ServerId}",
                request.ServerId);

            // Step 1: Create droplet
            var dropletSize = MapServerPlanToDropletSize(request.RamMb);
            var region = request.Region ?? _settings.DefaultRegion;

            var dropletRequest = new
            {
                name = $"gameserver-{request.ServerId:N}",
                region = region,
                size = dropletSize,
                image = "docker-20-04", // Ubuntu 20.04 with Docker pre-installed
                ssh_keys = string.IsNullOrEmpty(_settings.SshKeyId) 
                    ? Array.Empty<string>() 
                    : new[] { _settings.SshKeyId },
                backups = _settings.EnableBackups,
                ipv6 = true,
                monitoring = _settings.EnableMonitoring,
                tags = _settings.DefaultTags
                    .Concat(new[] { $"user-{request.UserId}", $"type-{request.ServerType}" })
                    .ToArray()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(dropletRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/droplets", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Failed to create droplet: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                
                return new ProvisionResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create droplet: {response.StatusCode}"
                };
            }

            var dropletResponse = await response.Content.ReadFromJsonAsync<DigitalOceanDropletResponse>();
            if (dropletResponse?.Droplet == null)
            {
                return new ProvisionResult
                {
                    Success = false,
                    ErrorMessage = "Invalid response from DigitalOcean API"
                };
            }

            var dropletId = dropletResponse.Droplet.Id;
            _logger.LogInformation(
                "Created droplet {DropletId} for server {ServerId}",
                dropletId, request.ServerId);

            // Step 2: Wait for droplet to become active
            var ipAddress = await WaitForDropletActiveAsync(dropletId);
            if (string.IsNullOrEmpty(ipAddress))
            {
                return new ProvisionResult
                {
                    Success = false,
                    ErrorMessage = "Droplet failed to become active"
                };
            }

            // Step 3: Assign default game port
            var port = GetDefaultPortForGame(request.ServerType);

            _logger.LogInformation(
                "Successfully provisioned droplet {DropletId} at {IpAddress}:{Port}",
                dropletId, ipAddress, port);

            return new ProvisionResult
            {
                Success = true,
                ProviderId = dropletId.ToString(),
                IpAddress = ipAddress,
                Port = port,
                ProvisionedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    ["region"] = region,
                    ["size"] = dropletSize,
                    ["image"] = "docker-20-04"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating droplet for server {ServerId}",
                request.ServerId);
            
            return new ProvisionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ServerInfo> GetServerStatusAsync(string providerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/droplets/{providerId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get droplet status: {StatusCode}",
                    response.StatusCode);
                
                return new ServerInfo
                {
                    ProviderId = providerId,
                    Status = "unknown",
                    IsHealthy = false
                };
            }

            var dropletResponse = await response.Content.ReadFromJsonAsync<DigitalOceanDropletResponse>();
            if (dropletResponse?.Droplet == null)
            {
                return new ServerInfo
                {
                    ProviderId = providerId,
                    Status = "unknown",
                    IsHealthy = false
                };
            }

            var droplet = dropletResponse.Droplet;
            var ipAddress = droplet.Networks?.V4?.FirstOrDefault()?.IpAddress;

            return new ServerInfo
            {
                ProviderId = providerId,
                Status = droplet.Status ?? "unknown",
                IsHealthy = droplet.Status == "active",
                IpAddress = ipAddress,
                CreatedAt = droplet.CreatedAt,
                Region = droplet.Region?.Slug
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting server status for {ProviderId}", providerId);
            
            return new ServerInfo
            {
                ProviderId = providerId,
                Status = "error",
                IsHealthy = false
            };
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StartServerAsync(string providerId)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { type = "power_on" }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"/droplets/{providerId}/actions",
                content);

            var success = response.IsSuccessStatusCode;
            
            _logger.LogInformation(
                "Start droplet {DropletId}: {Success}",
                providerId, success);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting droplet {ProviderId}", providerId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StopServerAsync(string providerId)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { type = "power_off" }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"/droplets/{providerId}/actions",
                content);

            var success = response.IsSuccessStatusCode;
            
            _logger.LogInformation(
                "Stop droplet {DropletId}: {Success}",
                providerId, success);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping droplet {ProviderId}", providerId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RestartServerAsync(string providerId)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { type = "reboot" }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"/droplets/{providerId}/actions",
                content);

            var success = response.IsSuccessStatusCode;
            
            _logger.LogInformation(
                "Restart droplet {DropletId}: {Success}",
                providerId, success);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting droplet {ProviderId}", providerId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteServerAsync(string providerId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/droplets/{providerId}");
            
            var success = response.IsSuccessStatusCode;
            
            _logger.LogInformation(
                "Delete droplet {DropletId}: {Success}",
                providerId, success);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting droplet {ProviderId}", providerId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ServerMetrics> GetServerMetricsAsync(string providerId)
    {
        try
        {
            // Note: DigitalOcean's monitoring API requires the droplet to have monitoring enabled
            // This is a simplified implementation
            var response = await _httpClient.GetAsync(
                $"/monitoring/metrics/droplet/cpu?host_id={providerId}&start={DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds()}&end={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

            if (!response.IsSuccessStatusCode)
            {
                return new ServerMetrics
                {
                    Timestamp = DateTime.UtcNow
                };
            }

            // Parse metrics response (simplified - actual implementation would parse JSON)
            return new ServerMetrics
            {
                CpuUsagePercent = 0,
                MemoryUsagePercent = 0,
                DiskUsagePercent = 0,
                NetworkInBytesPerSec = 0,
                NetworkOutBytesPerSec = 0,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for droplet {ProviderId}", providerId);
            
            return new ServerMetrics
            {
                Timestamp = DateTime.UtcNow
            };
        }
    }

    #region Private Helper Methods

    private string MapServerPlanToDropletSize(int ramMb)
    {
        return ramMb switch
        {
            <= 1024 => "s-1vcpu-1gb",      // $6/month
            <= 2048 => "s-1vcpu-2gb",      // $12/month
            <= 4096 => "s-2vcpu-4gb",      // $24/month
            <= 8192 => "s-4vcpu-8gb",      // $48/month
            <= 16384 => "s-6vcpu-16gb",    // $96/month
            _ => "s-8vcpu-32gb"            // $192/month
        };
    }

    private int GetDefaultPortForGame(string gameType)
    {
        return gameType.ToLower() switch
        {
            "minecraft" => 25565,
            "terraria" => 7777,
            "valheim" => 2456,
            "rust" => 28015,
            "ark" => 7777,
            "cs:go" or "csgo" => 27015,
            "7 days to die" => 26900,
            _ => 25565
        };
    }

    private async Task<string?> WaitForDropletActiveAsync(int dropletId)
    {
        var stopwatch = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(_settings.ProvisioningTimeoutSeconds);

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                var status = await GetServerStatusAsync(dropletId.ToString());
                
                if (status.Status == "active" && !string.IsNullOrEmpty(status.IpAddress))
                {
                    _logger.LogInformation(
                        "Droplet {DropletId} is active after {Elapsed}s",
                        dropletId, stopwatch.Elapsed.TotalSeconds);
                    
                    return status.IpAddress;
                }

                if (status.Status == "error" || status.Status == "archive")
                {
                    _logger.LogError(
                        "Droplet {DropletId} entered error state: {Status}",
                        dropletId, status.Status);
                    return null;
                }

                _logger.LogDebug(
                    "Droplet {DropletId} status: {Status}, waiting...",
                    dropletId, status.Status);

                // Wait 5 seconds before checking again
                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error checking droplet status, retrying...");
                await Task.Delay(5000);
            }
        }

        _logger.LogError(
            "Droplet {DropletId} did not become active within {Timeout}s",
            dropletId, timeout.TotalSeconds);
        
        return null;
    }

    #endregion

    #region DigitalOcean API Response Models

    private class DigitalOceanDropletResponse
    {
        public DigitalOceanDroplet? Droplet { get; set; }
    }

    private class DigitalOceanDroplet
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DigitalOceanRegion? Region { get; set; }
        public DigitalOceanNetworks? Networks { get; set; }
    }

    private class DigitalOceanRegion
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
    }

    private class DigitalOceanNetworks
    {
        public List<DigitalOceanNetwork>? V4 { get; set; }
        public List<DigitalOceanNetwork>? V6 { get; set; }
    }

    private class DigitalOceanNetwork
    {
        public string? IpAddress { get; set; }
        public string? Type { get; set; }
    }

    #endregion
}

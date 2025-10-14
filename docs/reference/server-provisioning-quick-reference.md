# Server Provisioning - Quick Reference

> Quick reference guide for implementing and working with the server provisioning system.

## Overview

Automated game server provisioning on DigitalOcean with support for multiple cloud providers through abstraction layer.

**Provisioning Time:** 4-6 minutes  
**Provider:** DigitalOcean (primary), AWS/Azure (future)  
**Job Processor:** Hangfire

---

## Key Components

```
Order Completed → Hangfire Job → Provisioning Service → Cloud Provider
                                                      → Update Database
                                                      → Send Notifications
```

---

## Server States

| State | Description | User Actions | Next State |
|-------|-------------|--------------|------------|
| `Pending` | Order placed | None | `Provisioning` |
| `Provisioning` | Creating infrastructure | None | `Configuring` |
| `Configuring` | Installing software | None | `Starting` |
| `Starting` | Launching game server | None | `Running` / `Failed` |
| `Running` | Active and accessible | Start, Stop, Restart, Delete | `Stopped`, `Terminated` |
| `Stopped` | Paused by user | Start, Delete | `Running`, `Terminated` |
| `Suspended` | Payment/expiry issue | Add payment | `Running`, `Terminated` |
| `Failed` | Provisioning failed | Delete | `Terminated` |
| `Terminated` | Permanently deleted | None | - |

---

## Core Interfaces

### ICloudProviderService

```csharp
public interface ICloudProviderService
{
    Task<ProvisionResult> CreateServerAsync(ServerProvisionRequest request);
    Task<ServerInfo> GetServerStatusAsync(string providerId);
    Task<bool> StartServerAsync(string providerId);
    Task<bool> StopServerAsync(string providerId);
    Task<bool> RestartServerAsync(string providerId);
    Task<bool> DeleteServerAsync(string providerId);
}
```

### IServerProvisioningService

```csharp
public interface IServerProvisioningService
{
    Task<Guid> ProvisionServerAsync(Guid orderId);
    Task<ProvisioningStatus> GetProvisioningStatusAsync(Guid serverId);
    Task<bool> RollbackProvisioningAsync(Guid serverId, string reason);
}
```

### IServerManagementService

```csharp
public interface IServerManagementService
{
    Task<bool> StartServerAsync(Guid serverId);
    Task<bool> StopServerAsync(Guid serverId);
    Task<bool> RestartServerAsync(Guid serverId);
    Task<bool> DeleteServerAsync(Guid serverId);
    Task<ServerHealthStatus> CheckServerHealthAsync(Guid serverId);
}
```

---

## Configuration

### appsettings.json

```json
{
  "CloudProviders": {
    "Default": "DigitalOcean",
    "DigitalOcean": {
      "ApiToken": "dop_v1_xxxxx",
      "ApiBaseUrl": "https://api.digitalocean.com/v2",
      "DefaultRegion": "nyc3",
      "SshKeyId": "12345678",
      "ProvisioningTimeoutSeconds": 300,
      "EnableMonitoring": true
    }
  },
  "Provisioning": {
    "MaxConcurrentProvisions": 10,
    "RetryAttempts": 3,
    "HealthCheckIntervalMinutes": 5,
    "AutoRollbackOnFailure": true
  }
}
```

### Dependency Injection (Program.cs)

```csharp
// Cloud provider services
builder.Services.Configure<DigitalOceanSettings>(
    builder.Configuration.GetSection("CloudProviders:DigitalOcean"));

builder.Services.AddHttpClient<ICloudProviderService, DigitalOceanProviderService>()
    .AddPolicyHandler(ProvisioningRetryPolicy.GetPolicy());

// Provisioning services
builder.Services.AddScoped<IServerProvisioningService, ServerProvisioningService>();
builder.Services.AddScoped<IServerManagementService, ServerManagementService>();
builder.Services.AddSingleton<ICloudProviderFactory, CloudProviderFactory>();

// Background jobs
builder.Services.AddHangfire(config => 
    config.UsePostgreSqlStorage(connectionString));

// Register recurring jobs
RecurringJob.AddOrUpdate<ServerHealthCheckJob>(
    "server-health-check",
    job => job.ExecuteAsync(),
    "*/5 * * * *" // Every 5 minutes
);
```

---

## Quick Implementation

### 1. Trigger Provisioning

```csharp
// In PaymentsController after payment confirmation
var serverId = await _provisioningService.ProvisionServerAsync(orderId);
BackgroundJob.Enqueue<ServerProvisioningJob>(job => job.ExecuteAsync(orderId));
```

### 2. Check Status

```csharp
// GET /api/servers/{serverId}
var server = await _context.GameServers.FindAsync(serverId);
return new ServerDto
{
    Id = server.Id,
    Status = server.Status,
    IpAddress = server.IpAddress,
    Port = server.Port,
    ProvisioningProgress = CalculateProgress(server.Status)
};
```

### 3. Control Server

```csharp
// POST /api/servers/{serverId}/start
await _serverManagement.StartServerAsync(serverId);

// POST /api/servers/{serverId}/stop
await _serverManagement.StopServerAsync(serverId);

// DELETE /api/servers/{serverId}
await _serverManagement.DeleteServerAsync(serverId);
```

---

## DigitalOcean Quick Reference

### Droplet Sizes

```csharp
private string MapServerPlanToDropletSize(ServerPlan plan)
{
    return plan.RAM switch
    {
        <= 1024 => "s-1vcpu-1gb",    // $6/mo
        <= 2048 => "s-1vcpu-2gb",    // $12/mo
        <= 4096 => "s-2vcpu-4gb",    // $24/mo
        <= 8192 => "s-4vcpu-8gb",    // $48/mo
        _ => "s-6vcpu-16gb"          // $96/mo
    };
}
```

### Create Droplet Request

```json
POST https://api.digitalocean.com/v2/droplets
Authorization: Bearer {token}

{
  "name": "gameserver-{guid}",
  "region": "nyc3",
  "size": "s-2vcpu-4gb",
  "image": "docker-20-04",
  "ssh_keys": [12345678],
  "monitoring": true,
  "tags": ["game-server", "user-{userId}"]
}
```

---

## Common Tasks

### Add New Cloud Provider

1. Create `AzureProviderService : ICloudProviderService`
2. Implement all interface methods
3. Register in `CloudProviderFactory`
4. Add configuration section
5. Update UI to allow provider selection

### Handle Provisioning Failure

```csharp
try
{
    await _provisioningService.ProvisionServerAsync(orderId);
}
catch (ProvisioningException ex)
{
    // Automatic rollback is enabled
    _logger.LogError(ex, "Provisioning failed");
    await _emailService.SendProvisioningFailedNotificationAsync(serverId);
    // Hangfire will retry 3x automatically
}
```

### Monitor Provisioning

```csharp
// Query slow provisions
var slowProvisions = await _context.GameServers
    .Where(s => s.Status == ServerStatus.Provisioning)
    .Where(s => s.ProvisioningStartedAt < DateTime.UtcNow.AddMinutes(-10))
    .ToListAsync();

// Alert on failures
var failureRate = await _context.GameServers
    .Where(s => s.CreatedAt > DateTime.UtcNow.AddHours(-24))
    .CountAsync(s => s.Status == ServerStatus.Failed) * 100.0 / totalCount;

if (failureRate > 5) // Alert if > 5% failure rate
    await _alertService.SendAlertAsync("High provisioning failure rate");
```

---

## API Endpoints

```http
# Provisioning status
GET /api/servers/{serverId}/provisioning-status

# Server control
POST /api/servers/{serverId}/start
POST /api/servers/{serverId}/stop
POST /api/servers/{serverId}/restart
DELETE /api/servers/{serverId}

# Server details
GET /api/servers/{serverId}
GET /api/servers/user/{userId}

# Admin
POST /api/admin/servers/provision (manual trigger)
GET /api/admin/servers/provisioning-queue
```

---

## Database Schema

```sql
-- Key columns in game_servers table
provider_type VARCHAR(50) DEFAULT 'digitalocean'
provider_id VARCHAR(255)              -- Droplet ID
provider_region VARCHAR(50)           -- nyc3, sfo2, etc
provisioning_started_at TIMESTAMP
provisioning_completed_at TIMESTAMP
provisioning_error TEXT
ssh_key_private TEXT                  -- Encrypted
last_health_check_at TIMESTAMP
health_check_status VARCHAR(20)
```

---

## Error Codes

| Code | Meaning | Action |
|------|---------|--------|
| `PROV_001` | Droplet creation failed | Retry 3x, then rollback |
| `PROV_002` | SSH connection timeout | Check network, retry |
| `PROV_003` | Docker install failed | Retry once, then rollback |
| `PROV_004` | Container start failed | Restart 2x, then rollback |
| `PROV_005` | Health check failed | Alert admin |
| `PROV_006` | Provisioning timeout | Rollback and refund |

---

## Testing

### Unit Tests

```csharp
[Fact]
public async Task ProvisionServerAsync_ValidOrder_CreatesDroplet()
{
    // Arrange
    var mockProvider = new Mock<ICloudProviderService>();
    mockProvider.Setup(p => p.CreateServerAsync(It.IsAny<ServerProvisionRequest>()))
        .ReturnsAsync(new ProvisionResult { Success = true });
    
    // Act
    var result = await _provisioningService.ProvisionServerAsync(orderId);
    
    // Assert
    Assert.NotEqual(Guid.Empty, result);
}
```

### Integration Tests

```csharp
[Fact]
public async Task EndToEnd_ProvisionServer_Success()
{
    // Create order → Provision → Verify running
    var order = await CreateTestOrderAsync();
    await _provisioningJob.ExecuteAsync(order.Id);
    
    var server = await _context.GameServers
        .FirstAsync(s => s.OrderId == order.Id);
    
    Assert.Equal(ServerStatus.Running, server.Status);
    Assert.NotNull(server.IpAddress);
}
```

---

## Performance Targets

- **Provisioning Time:** < 6 minutes (p95)
- **Success Rate:** > 95%
- **Rollback Time:** < 2 minutes
- **Health Check:** Every 5 minutes
- **Max Concurrent:** 10 provisions

---

## Resources

- **Full Design:** [server-provisioning.md](../features/server-provisioning.md)
- **DigitalOcean API:** https://docs.digitalocean.com/reference/api/
- **Hangfire Docs:** https://docs.hangfire.io/

---

**Last Updated:** 2025-10-14  
**Status:** ✅ Ready for Implementation

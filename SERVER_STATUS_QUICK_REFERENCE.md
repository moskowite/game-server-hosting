# Server Status Tracking - Quick Reference

## Files Created

### Services (6 files)
1. **IServerManagementService.cs** - Interface for server lifecycle management
2. **ServerManagementService.cs** - Implementation with start/stop/restart/delete/health check
3. **IServerStatusNotificationService.cs** - Interface for status change notifications
4. **ServerStatusNotificationService.cs** - Email notification implementation

### Controllers (1 file)
5. **GameServersController.cs** - RESTful API endpoints for server management

### Jobs (2 files)
6. **ServerHealthCheckJob.cs** - Background job for periodic health checks
7. **ServerProvisioningJob.cs** - Background job for server provisioning

### Documentation (2 files)
8. **SERVER_STATUS_TRACKING_IMPLEMENTATION.md** - Comprehensive documentation
9. **game-servers-tests.http** - HTTP test file for API endpoints

### Modified Files
- **Program.cs** - Added service registrations for new services

## API Endpoints

```
GET    /api/servers                 - List all current user's servers
GET    /api/servers/user/{userId}   - List servers for specific user (requires admin or own user)
GET    /api/servers/{id}            - Get server details
GET    /api/servers/{id}/status     - Get provisioning status
POST   /api/servers/{id}/start      - Start server
POST   /api/servers/{id}/stop       - Stop server
POST   /api/servers/{id}/restart    - Restart server
DELETE /api/servers/{id}            - Delete server
GET    /api/servers/{id}/health     - Get health status
GET    /api/servers/{id}/metrics    - Get resource metrics
```

## Key Features

✅ **ServerStatus Enum** - Already existed with all necessary states (Pending → Running → Stopped → Terminated)

✅ **Status Update Mechanism**
- `UpdateServerStatusAsync()` method in ServerManagementService
- Automatic timestamp updates
- Triggers notifications on status changes

✅ **Health Checks**
- `CheckServerHealthAsync()` performs health checks
- `ServerHealthCheckJob` runs periodic checks (every 5 minutes)
- Integrates with cloud provider API
- Updates database with health status

✅ **Status Change Notifications**
- Email notifications on status changes
- Contextual messages for each status
- Health alert notifications
- HTML-formatted emails with server details

✅ **Server Management**
- Start/Stop/Restart operations
- State validation (can't start a running server)
- Cloud provider integration
- Automatic error handling and rollback

## Configuration Required

### 1. Background Job Scheduler
Add Hangfire or similar job scheduler to run health checks:

```csharp
// Add to Program.cs
builder.Services.AddHangfire(config => /* ... */);

// Schedule health check job
RecurringJob.AddOrUpdate<ServerHealthCheckJob>(
    "server-health-check",
    job => job.ExecuteAsync(),
    "*/5 * * * *" // Every 5 minutes
);
```

### 2. Configuration Settings
Ensure these sections exist in appsettings.json:

```json
{
  "Provisioning": {
    "HealthCheckIntervalMinutes": 5,
    "SendNotifications": true,
    "DefaultProvider": "DigitalOcean"
  },
  "CloudProviders": {
    "DigitalOcean": {
      "ApiToken": "YOUR_TOKEN",
      "EnableMonitoring": true
    }
  }
}
```

## Testing

Use the `game-servers-tests.http` file with your favorite HTTP client (VS Code REST Client, Postman, etc.):

1. Get your JWT token from login
2. Replace `{{token}}` in the test file
3. Replace `{{serverId}}` with an actual server ID
4. Run the requests

## Next Steps

1. **Install Hangfire** (if not already installed)
   ```bash
   dotnet add package Hangfire.AspNetCore
   dotnet add package Hangfire.PostgreSql
   ```

2. **Configure Hangfire** in Program.cs

3. **Test the endpoints** using the HTTP test file

4. **Monitor logs** for health check results and status changes

5. **Set up alerting** for critical issues (optional)

## Architecture

```
Controller (GameServersController)
    ↓
Service Layer (ServerManagementService)
    ↓
Cloud Provider (DigitalOceanProviderService)
    ↓
Database Updates + Notifications
```

## Status Flow

```
Pending → Provisioning → Configuring → Starting → Running
                                                     ↓
                                         Stopped ←──┘
                                            ↓
                                       Terminated
```

Failed status can occur at any provisioning stage and leads to Terminated.

## Security

- All endpoints require JWT authentication
- Ownership verification on all operations
- Proper error messages without information disclosure
- Audit logging for all operations

## Performance

- Async/await throughout
- Background jobs for heavy operations
- Efficient database queries with Include
- Health checks run periodically (not on-demand)

---

For detailed information, see **SERVER_STATUS_TRACKING_IMPLEMENTATION.md**

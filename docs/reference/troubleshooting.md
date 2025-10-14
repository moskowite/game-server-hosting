# Troubleshooting Guide

Common issues and their solutions for the Game Server Hosting platform.

## 🔍 Quick Diagnosis

### Is the problem with...?

- **[PostgreSQL / Database](#database-issues)** - Connection errors, migration failures
- **[API](#api-issues)** - API won't start, endpoint errors
- **[Client](#client-issues)** - Client won't start, can't connect to API
- **[Docker](#docker-issues)** - Container problems
- **[Build / Compilation](#build-issues)** - Build failures, locked files
- **[VS Code / Debugging](#vscode-issues)** - Launch config problems

---

## Database Issues

### PostgreSQL Connection Failed

**Error**: `Failed to connect to 127.0.0.1:5432`

**Causes & Solutions**:

1. **PostgreSQL not running**
   ```bash
   # Check if running
   docker ps
   
   # If not listed, start it
   docker-compose up -d postgres
   
   # Verify it's healthy
   docker ps | grep gameserver-postgres
   ```

2. **Docker Desktop not running**
   - Start Docker Desktop
   - Wait for it to fully initialize (whale icon in system tray)
   - Retry connection

3. **Wrong credentials**
   - Check `appsettings.Development.json`
   - Default: `postgres/postgres`
   - Verify: `docker exec gameserver-postgres psql -U postgres -c "SELECT 1;"`

### Migration Errors

**Error**: `function uuid_generate_v4() does not exist`

**Solution**:
```bash
# Enable UUID extension
docker exec gameserver-postgres psql -U postgres -d gameserverhosting_dev -c 'CREATE EXTENSION IF NOT EXISTS "uuid-ossp";'

# Rerun migrations
dotnet ef database update --project src/GameServerHosting.Api
```

**Error**: `Migration already applied` or `Migration not found`

**Solution**:
```bash
# Check migration status
dotnet ef migrations list --project src/GameServerHosting.Api

# Reset database (WARNING: loses data)
dotnet ef database drop --project src/GameServerHosting.Api
dotnet ef database update --project src/GameServerHosting.Api
```

**Error**: `Pending model changes`

**Solution**:
```bash
# Create a new migration for the changes
dotnet ef migrations add <DescriptiveName> --project src/GameServerHosting.Api

# Apply it
dotnet ef database update --project src/GameServerHosting.Api
```

### Database Seeding Issues

**Error**: Duplicate key violations during seeding

**Solution**:
```bash
# Drop and recreate
dotnet ef database drop --project src/GameServerHosting.Api
dotnet ef database update --project src/GameServerHosting.Api

# The seeder runs automatically on startup
dotnet run --project src/GameServerHosting.Api
```

---

## API Issues

### API Won't Start

**Error**: `Port 5292 already in use`

**Solution**:
```bash
# Find what's using the port
netstat -ano | findstr :5292

# Kill the process (replace <PID> with actual ID)
taskkill /PID <PID> /F

# Restart API
dotnet run --project src/GameServerHosting.Api
```

**Error**: `The host was aborted`

**Cause**: Usually database connection issues

**Solution**:
1. Check PostgreSQL is running
2. Check connection string in `appsettings.Development.json`
3. Check database exists
4. Check migrations are applied

### Swagger Not Loading

**Issue**: `/swagger` shows 404

**Solution**:
```csharp
// Verify appsettings.Development.json has:
{
  "Swagger": {
    "Enabled": true
  }
}

// Verify Program.cs has:
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

### JWT Token Errors

**Error**: `Unauthorized` or `Invalid token`

**Solution**:
1. Check token hasn't expired
2. Verify `JwtSettings` in `appsettings.Development.json`
3. Ensure `Authorization: Bearer <token>` header is set
4. Token must be generated from `/api/auth/login`

---

## Client Issues

### Client Won't Start

**Error**: `Port 5106 already in use`

**Solution**:
```bash
# Find and kill process
netstat -ano | findstr :5106
taskkill /PID <PID> /F

# Restart client
dotnet run --project src/GameServerHosting.Client
```

**Error**: `hostpolicy.dll not found`

**Solution**:
```bash
# Clean and rebuild
dotnet clean src/GameServerHosting.Client
dotnet build src/GameServerHosting.Client
dotnet run --project src/GameServerHosting.Client
```

### Client Can't Connect to API

**Issue**: API calls fail with network errors

**Solutions**:

1. **Verify API is running**
   ```bash
   # Check API at:
   curl http://localhost:5292/api/health
   # Or open: http://localhost:5292/swagger
   ```

2. **Check Client configuration**
   ```bash
   # Verify src/GameServerHosting.Client/wwwroot/appsettings.Development.json
   {
     "ApiSettings": {
       "BaseUrl": "http://localhost:5292"  # Must match API URL
     }
   }
   ```

3. **CORS issues**
   - Check browser console for CORS errors
   - Verify `appsettings.Development.json` in API has client URL in `Cors.AllowedOrigins`
   - Should include: `"http://localhost:5106"`

4. **Rebuild client after config changes**
   ```bash
   dotnet clean src/GameServerHosting.Client
   dotnet build src/GameServerHosting.Client
   ```

---

## Docker Issues

### Docker Desktop Not Starting

**Solutions**:
1. Restart Docker Desktop
2. Check if Hyper-V is enabled (Windows)
3. Check if WSL 2 is installed and updated (Windows)
4. Restart computer if necessary

### Container Won't Start

**Error**: `Container already exists`

**Solution**:
```bash
# Remove existing container
docker rm gameserver-postgres

# Recreate
docker-compose up -d postgres
```

**Error**: `port is already allocated`

**Solution**:
```bash
# Find what's using port 5432
netstat -ano | findstr :5432

# Kill the process or stop the container
docker stop gameserver-postgres

# Restart
docker-compose up -d postgres
```

### Container Crashes Immediately

**Solution**:
```bash
# Check logs
docker logs gameserver-postgres

# Common issues:
# - Insufficient memory
# - Corrupted volume
# - Port conflict

# Nuclear option: Remove everything and start fresh
docker-compose down -v
docker-compose up -d postgres
```

---

## Build Issues

### Files Locked / Can't Build

**Error**: `file is being used by another process`

**Solution**:
1. **Stop all running instances**
   - Press `Shift+F5` in VS Code to stop debugger
   - Close all terminal instances running `dotnet run` or `dotnet watch`
   
2. **Kill .NET processes**
   ```bash
   # Find .NET processes
   tasklist | findstr dotnet
   
   # Kill them (replace <PID>)
   taskkill /PID <PID> /F
   ```

3. **Clean and rebuild**
   ```bash
   dotnet clean
   dotnet build
   ```

### NuGet Package Restore Failures

**Error**: Package restore failed

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# If still failing, delete bin/obj folders
Remove-Item -Recurse -Force **/bin, **/obj

# Restore again
dotnet restore
```

### Compilation Errors After Git Pull

**Solution**:
```bash
# Clean everything
dotnet clean

# Restore packages (in case dependencies changed)
dotnet restore

# Rebuild
dotnet build

# Update database (in case migrations were added)
dotnet ef database update --project src/GameServerHosting.Api
```

---

## VS Code Issues

### Launch Configuration Not Working

**Issue**: F5 doesn't launch or shows errors

**Solutions**:

1. **Verify `.vscode/launch.json` exists**
   - Should be in project root
   - Check it has configurations for API and Client

2. **PostgreSQL auto-start fails**
   - Manually start: `docker-compose up -d postgres`
   - Check Docker Desktop is running

3. **Build task fails**
   - Run manually: `dotnet build src/GameServerHosting.Api`
   - Check for compilation errors

### Debugger Won't Attach

**Issue**: Breakpoints show as hollow circles

**Solutions**:
1. Ensure you're running in Debug mode (not Release)
2. Check PDB files are being generated
3. Restart VS Code
4. Clean and rebuild

### Multiple Instances Running

**Issue**: Multiple API or Client instances running

**Solution**:
```bash
# List all .NET processes
tasklist | findstr dotnet

# Kill all (nuclear option)
taskkill /F /IM dotnet.exe

# Or use VS Code: Click "Stop All" button
```

---

## Performance Issues

### Slow API Responses

**Diagnosis**:
1. Check Serilog output for slow queries
2. Enable SQL logging in `appsettings.Development.json`:
   ```json
   "Logging": {
     "LogLevel": {
       "Microsoft.EntityFrameworkCore.Database.Command": "Information"
     }
   }
   ```

**Solutions**:
- Add database indexes for frequently queried fields
- Use `.AsNoTracking()` for read-only queries
- Implement caching for expensive operations

### Client Loads Slowly

**Common Causes**:
- Large Blazor WebAssembly bundle
- Too many API calls on page load
- No loading indicators

**Solutions**:
- Implement lazy loading for routes
- Add loading skeletons
- Batch API calls
- Use pagination for lists

---

## Testing Issues

### Tests Failing After Code Changes

**Solution**:
1. Update test data/mocks to match new code
2. Check if database schema changed (update test setup)
3. Verify test database is clean between runs

### Integration Tests Can't Connect to Database

**Solution**:
```bash
# Ensure test database exists
# Tests should use a separate database name like:
"gameserverhosting_test"

# Or use in-memory database for unit tests
```

---

## Getting More Help

### Enable Detailed Logging

Add to `appsettings.Development.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Debug",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    }
  }
}
```

### Check Application Logs

**API Logs**: 
- Console output
- `logs/dev/gameserver-*.log`

**Client Logs**:
- Browser console (F12)
- Network tab for API calls

### Diagnostic Checklist

When reporting issues, include:
- [ ] Exact error message
- [ ] Steps to reproduce
- [ ] What you expected vs what happened
- [ ] Environment (OS, .NET version, Docker version)
- [ ] Recent changes (git commit, migrations, etc.)
- [ ] Relevant logs

### Community Resources

- **GitHub Issues**: Check existing issues or create new one
- **Stack Overflow**: Tag with `asp.net-core`, `blazor`, `ef-core`
- **Documentation**: Check [docs/README.md](../README.md) for guides

---

## Emergency Procedures

### Nuclear Reset (Clean Slate)

⚠️ **WARNING**: This will delete all local data!

```bash
# 1. Stop everything
docker-compose down -v
taskkill /F /IM dotnet.exe

# 2. Clean build artifacts
Remove-Item -Recurse -Force **/bin, **/obj

# 3. Clear NuGet cache
dotnet nuget locals all --clear

# 4. Restore and rebuild
dotnet restore
dotnet build

# 5. Restart database and apply migrations
docker-compose up -d postgres
dotnet ef database update --project src/GameServerHosting.Api

# 6. Run the application
dotnet run --project src/GameServerHosting.Api
```

### Backup Before Major Changes

```bash
# Backup database
docker exec gameserver-postgres pg_dump -U postgres gameserverhosting_dev > backup.sql

# Restore if needed
docker exec -i gameserver-postgres psql -U postgres gameserverhosting_dev < backup.sql
```

---

**Still stuck?** Check the [Quick Start Guide](../setup/quick-start.md) or [Local Development Setup](../setup/local-development.md) for detailed instructions.


# VS Code Configuration & Debugging

Complete guide to using VS Code/Cursor for developing the Game Server Hosting platform, including launch configurations, tasks, and PostgreSQL auto-start.

## 🚀 Quick Start

1. Open the project in VS Code or Cursor
2. Press `F5`
3. Select "Full Stack (API + Client)"
4. Everything launches automatically (including PostgreSQL)!

## 📋 Available Launch Configurations

### Individual Configurations

**1. API (Backend)**
- Runs on `http://localhost:5292`
- **Automatically starts PostgreSQL** if not running
- Pre-builds the API project
- Opens Swagger UI automatically

**2. Client (Frontend)**
- Runs on `http://localhost:5106`
- Opens the app in your browser
- Pre-builds the Client project

**3. Attach to API**
- Attach debugger to running API process
- Use when API is already running via terminal

**4. Attach to Client**
- Attach debugger to running Client process
- Use when Client is already running via terminal

### Compound Configuration

**5. Full Stack (API + Client)** ⭐ Recommended
- **Automatically starts PostgreSQL** if not running
- Launches API first, then Client
- Opens both Swagger and Client UI
- Perfect for full-stack development
- Stop all with `Shift+F5`

## 🐘 PostgreSQL Auto-Start

### How It Works

When you launch the API or Full Stack configuration:

1. **Pre-launch task runs** (`build-api-with-db`)
2. **Checks if PostgreSQL is running**
3. **Starts PostgreSQL automatically** if needed via `docker-compose up -d postgres`
4. **Builds the API project**
5. **Launches the application**

### Smart Detection

The `ensure-postgres` task:
- ✅ **If running**: Does nothing (fast!)
- ✅ **If not running**: Starts automatically
- ✅ **Idempotent**: Safe to run multiple times

**This means you can press F5 repeatedly without errors!**

### Usage Scenarios

**Scenario 1: Normal Development** (Most Common)
```
1. Ensure Docker Desktop is running
2. Press F5
3. Select "Full Stack (API + Client)"
4. Done! PostgreSQL starts automatically
```

**Scenario 2: Manual Database Control**
```
1. Ctrl+Shift+P → "Tasks: Run Task" → "start-postgres"
2. Wait for container to start
3. Press F5 to launch your app
```

**Scenario 3: Stop Everything**
```
1. Shift+F5 to stop debugger
2. Ctrl+Shift+P → "Tasks: Run Task" → "stop-postgres"
```

**Scenario 4: PostgreSQL Already Running**
```
1. PostgreSQL container already running
2. Press F5
3. Auto-detection skips startup
4. Launches immediately!
```

## 🛠️ Available Tasks

Access tasks via `Ctrl+Shift+P` → "Tasks: Run Task"

### Database Tasks

| Task | Purpose |
|------|---------|
| `start-postgres` | Start PostgreSQL container manually |
| `stop-postgres` | Stop and remove PostgreSQL container |
| `ensure-postgres` | Check if running, start if not (used by launch configs) |

### Build Tasks

| Task | Purpose |
|------|---------|
| `build-api` | Build the API project |
| `build-client` | Build the Client project |
| `build-api-with-db` | Start PostgreSQL + build API (automatic) |
| `watch-api` | Run API with hot reload |
| `watch-client` | Run Client with hot reload |
| `clean-api` | Clean API build artifacts |
| `clean-client` | Clean Client build artifacts |
| `run-client` | Run client via dotnet run |

## 🎯 How to Use

### Option 1: Debug Panel (Recommended)

1. Open **Run and Debug** panel (`Ctrl+Shift+D` or `Cmd+Shift+D`)
2. Select configuration from dropdown
3. Click green play button or press `F5`

### Option 2: Command Palette

1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P`)
2. Type "Debug: Select and Start Debugging"
3. Choose your configuration

### Option 3: Quick Launch

- Press `F5` to launch last used configuration
- Press `Ctrl+F5` (or `Cmd+F5`) to run without debugging

## 🐛 Debugging Tips

### Setting Breakpoints

1. Click in the left margin of any code file
2. Red dot appears = breakpoint set
3. Run with debugger (`F5`)
4. Execution pauses at breakpoints

### Debug Tools

**Variables Panel**: Inspect current variable values  
**Call Stack**: View execution path  
**Debug Console**: Evaluate expressions  
**Watch Panel**: Monitor specific variables  
**Stop All**: Terminate all compound processes

### Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Start Debugging | `F5` |
| Run Without Debugging | `Ctrl+F5` |
| Stop Debugging | `Shift+F5` |
| Restart Debugging | `Ctrl+Shift+F5` |
| Step Over | `F10` |
| Step Into | `F11` |
| Step Out | `Shift+F11` |
| Continue | `F5` |

## ⚙️ Environment Configuration

### API Configuration (Environment-Based)

The Client uses environment-specific configuration files:

- **Development**: `wwwroot/appsettings.Development.json` → `http://localhost:5292`
- **Staging**: `wwwroot/appsettings.Staging.json` → `https://api-staging.gameserverhosting.com`
- **Production**: `wwwroot/appsettings.json` → `https://api.gameserverhosting.com`

**No code changes needed when deploying!**

### How It Works

```csharp
// In Program.cs
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() 
    ?? new ApiSettings { BaseUrl = builder.HostEnvironment.BaseAddress };

builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiSettings.BaseUrl) 
});
```

### Environment Variables

All launch configurations use `ASPNETCORE_ENVIRONMENT=Development` by default:
- ✅ Enables Swagger UI
- ✅ Uses Development settings
- ✅ Provides detailed error messages
- ✅ Uses local SMTP (port 1025)

## 🔧 Technical Details

### PostgreSQL Auto-Start Task

The `ensure-postgres` task uses:

```powershell
docker ps --filter name=gameserver-postgres --filter status=running --format '{{.Names}}' | findstr gameserver-postgres || docker-compose up -d postgres
```

**How it works:**
1. Lists running containers named `gameserver-postgres`
2. Checks if the name appears in output  
3. If not found, starts via `docker-compose`
4. Runs in detached mode

### Launch Configuration Structure

```json
{
  "name": "API (Backend)",
  "type": "coreclr",
  "request": "launch",
  "preLaunchTask": "build-api-with-db",  // Auto-starts PostgreSQL
  "program": "${workspaceFolder}/src/GameServerHosting.Api/bin/Debug/net8.0/GameServerHosting.Api.dll",
  "serverReadyAction": {
    "action": "openExternally",
    "uriFormat": "%s/swagger"
  }
}
```

### Compound Configuration

```json
{
  "name": "Full Stack (API + Client)",
  "configurations": [
    "API (Backend)",    // Launches first
    "Client (Frontend)" // Launches second
  ],
  "stopAll": true  // Shift+F5 stops both
}
```

## 🐛 Troubleshooting

### API Fails to Start

**PostgreSQL not running:**
```bash
# Check Docker Desktop is running
docker ps

# Manually start if auto-start fails
docker-compose up -d postgres

# Verify it's healthy
docker ps | grep gameserver-postgres
```

**Port 5292 in use:**
```bash
# Find what's using the port
netstat -ano | findstr :5292

# Kill the process
taskkill /PID <PID> /F
```

### Client Fails to Start

**Port 5106 in use:**
```bash
netstat -ano | findstr :5106
taskkill /PID <PID> /F
```

**Build errors:**
```bash
dotnet clean src/GameServerHosting.Client
dotnet build src/GameServerHosting.Client
```

### Client Can't Reach API

1. Verify API is running: `http://localhost:5292/swagger`
2. Check Client config: `src/GameServerHosting.Client/wwwroot/appsettings.Development.json`
3. Ensure `BaseUrl` is `http://localhost:5292`
4. Check CORS settings in API
5. Rebuild Client after config changes

### Hot Reload Not Working

- Use watch tasks: `watch-api` or `watch-client`
- Or run from terminal: `dotnet watch run --project <project-path>`

### Debugger Won't Attach

1. Ensure running in Debug mode (not Release)
2. Check PDB files are generated
3. Restart VS Code
4. Clean and rebuild

### Multiple Instances Running

```bash
# List all .NET processes
tasklist | findstr dotnet

# Kill all
taskkill /F /IM dotnet.exe

# Or use VS Code "Stop All" button
```

## 📝 Prerequisites Checklist

Before using launch configurations:

- [ ] Docker Desktop is running
- [ ] Database migrations applied (first time)
  ```bash
  dotnet ef database update --project src/GameServerHosting.Api
  ```
- [ ] NuGet packages restored
  ```bash
  dotnet restore
  ```

## 🎯 Next Steps

After successful launch:

1. **API (Swagger)**: Test endpoints at `http://localhost:5292/swagger`
2. **Client**: Browse the app at `http://localhost:5106`
3. **Test Integration**: Register a user, login, view dashboard

## 📚 Related Documentation

- [Quick Start Guide](quick-start.md) - First-time setup
- [Local Development](local-development.md) - Complete dev guide
- [Database Setup](database-setup.md) - PostgreSQL and migrations
- [Troubleshooting](../reference/troubleshooting.md) - Common issues

---

**Pro Tip**: The Full Stack configuration with PostgreSQL auto-start means you can go from cold start to full development environment in under 30 seconds! 🚀


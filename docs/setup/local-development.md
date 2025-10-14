# Local Development Setup

Complete guide for setting up and running the Game Server Hosting platform locally.

## 📋 Prerequisites

Before you begin, ensure you have:

- ✅ **.NET 8 SDK** (8.0.414 or later)
- ✅ **Docker Desktop** installed and running
- ✅ **VS Code or Cursor** (recommended IDE)
- ✅ **Git** for version control

## 🚀 Complete Setup

### Step 1: Clone the Repository

```bash
git clone <repository-url>
cd game-server-hosting
```

### Step 2: Install .NET Tools

```bash
# Install Entity Framework Core tools
dotnet tool install --global dotnet-ef

# Verify installation
dotnet --version
dotnet ef --version
```

### Step 3: Restore NuGet Packages

```bash
dotnet restore
```

### Step 4: Start PostgreSQL

```bash
# Start PostgreSQL container
docker-compose up -d postgres

# Verify it's running and healthy
docker ps
```

### Step 5: Configure Database

```bash
# Enable UUID extension
docker exec gameserver-postgres psql -U postgres -d gameserverhosting_dev -c 'CREATE EXTENSION IF NOT EXISTS "uuid-ossp";'

# Apply migrations
dotnet ef database update --project src/GameServerHosting.Api
```

### Step 6: Build the Solution

```bash
dotnet build
```

### Step 7: Run the Application

**Option A: VS Code Launch (Recommended)**
1. Open in VS Code/Cursor
2. Press `F5`
3. Select "Full Stack (API + Client)"

**Option B: Command Line**
```bash
# Terminal 1
dotnet run --project src/GameServerHosting.Api

# Terminal 2
dotnet run --project src/GameServerHosting.Client
```

## ✅ Verification

After setup, verify everything works:

- ✅ **API**: Open `http://localhost:5292/swagger`
- ✅ **Client**: Open `http://localhost:5106`
- ✅ **PostgreSQL**: Running on port 5432
- ✅ **Database**: Migrations applied

## 🎯 Development Workflow

### Daily Workflow

```bash
# 1. Start Docker Desktop (if not running)

# 2. Launch the application
#    In VS Code: Press F5 → "Full Stack"
#    PostgreSQL auto-starts!

# 3. Make changes and test

# 4. Stop when done: Shift+F5
```

### With Hot Reload

```bash
# Terminal 1 - API with hot reload
dotnet watch run --project src/GameServerHosting.Api

# Terminal 2 - Client with hot reload
dotnet watch run --project src/GameServerHosting.Client
```

Changes apply automatically without restart!

## 🔧 Configuration

### API Configuration

**File**: `src/GameServerHosting.Api/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gameserverhosting_dev;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "DevSecretKey_ThisIsOnlyForDevelopment_MustBeAtLeast32Characters!",
    "ExpirationInMinutes": 120
  },
  "EmailSettings": {
    "Provider": "SMTP",
    "FromEmail": "dev@gameserverhosting.local",
    "Smtp": {
      "Host": "localhost",
      "Port": 1025,
      "EnableSsl": false
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5106"  // Client URL
    ]
  }
}
```

### Client Configuration

**File**: `src/GameServerHosting.Client/wwwroot/appsettings.Development.json`

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5292"  // API URL
  }
}
```

### Environment Variables

All launch configurations use:
- `ASPNETCORE_ENVIRONMENT=Development`

This enables:
- Swagger UI
- Detailed error messages
- Development logging
- Local SMTP server

## 📝 Common Development Tasks

### Building

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/GameServerHosting.Api
dotnet build src/GameServerHosting.Client

# Clean build artifacts
dotnet clean
```

### Testing

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run specific test
dotnet test --filter "TestName"
```

### Database Operations

```bash
# Create migration
dotnet ef migrations add <Name> --project src/GameServerHosting.Api

# Apply migrations
dotnet ef database update --project src/GameServerHosting.Api

# Remove last migration
dotnet ef migrations remove --project src/GameServerHosting.Api

# Drop database
dotnet ef database drop --project src/GameServerHosting.Api
```

### Docker Operations

```bash
# Start database
docker-compose up -d postgres

# Stop database
docker-compose down

# View logs
docker-compose logs -f postgres

# Restart
docker-compose restart postgres

# Execute commands in container
docker exec -it gameserver-postgres psql -U postgres
```

## 🏗️ Project Structure

```
game-server-hosting/
├── src/
│   ├── GameServerHosting.Api/          # Backend API
│   │   ├── Controllers/                # API controllers
│   │   ├── Data/                       # DbContext & migrations
│   │   ├── Models/                     # DTOs and request models
│   │   ├── Services/                   # Business logic
│   │   └── Program.cs                  # Application startup
│   ├── GameServerHosting.Client/       # Frontend client
│   │   ├── Pages/                      # Blazor pages
│   │   ├── Services/                   # Client services
│   │   ├── Models/                     # Client models
│   │   └── wwwroot/                    # Static files & config
│   └── GameServerHosting.Shared/       # Shared code
│       ├── Entities/                   # Domain entities
│       └── Enums/                      # Shared enumerations
├── .vscode/                            # VS Code config
├── docs/                               # Documentation
├── docker-compose.yml                  # Docker services
└── GameServerHosting.sln              # Solution file
```

## 🎨 IDE Setup

### VS Code Extensions (Recommended)

- **C# Dev Kit** - C# language support
- **C#** - IntelliSense and debugging
- **.NET Extension Pack** - Complete .NET tooling
- **Docker** - Docker file support
- **GitLens** - Enhanced Git integration

### Cursor-Specific

Cursor includes C# support by default. No additional extensions needed!

## 🔍 Debugging

### Setting Breakpoints

1. Click left margin of code file
2. Red dot appears
3. Press `F5` to start debugging
4. Execution pauses at breakpoint

### Debug Panel

- **Variables**: Inspect current values
- **Watch**: Monitor specific expressions
- **Call Stack**: View execution path
- **Debug Console**: Evaluate code

### Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Start/Continue | `F5` |
| Step Over | `F10` |
| Step Into | `F11` |
| Step Out | `Shift+F11` |
| Stop | `Shift+F5` |
| Restart | `Ctrl+Shift+F5` |

## 🧪 Testing Locally

### API Testing

**Via Swagger UI:**
1. Run the API
2. Open `http://localhost:5292/swagger`
3. Try endpoints interactively

**Via HTTP Files:**
The project includes `.http` files for testing:
- `server-types-tests.http`
- `server-plans-tests.http`
- `email-verification-tests.http`
- `user-profile-tests.http`

Use the REST Client extension to run these.

### Client Testing

1. Run the Client
2. Open `http://localhost:5106`
3. Test features in the browser
4. Use browser DevTools (F12) for debugging

### Integration Testing

1. Launch "Full Stack" configuration
2. Register a new user
3. Login
4. Browse server catalog
5. View dashboard

## 🔄 Workflow Best Practices

### Before Starting Work

```bash
# Pull latest changes
git pull

# Restore packages (if changed)
dotnet restore

# Apply any new migrations
dotnet ef database update --project src/GameServerHosting.Api
```

### While Developing

1. **Use hot reload** for faster iteration
2. **Set breakpoints** for debugging
3. **Check Swagger** for API changes
4. **Monitor logs** for errors
5. **Test frequently** in browser

### Before Committing

```bash
# Build to check for errors
dotnet build

# Run tests
dotnet test

# Check for linting issues
# (if linter is configured)
```

## 🐛 Common Issues

### PostgreSQL Won't Start

**Solution**: See [Database Setup Guide](database-setup.md) or [Docker Setup Guide](docker-setup.md)

### Port Conflicts

```bash
# API port (5292)
netstat -ano | findstr :5292

# Client port (5106)
netstat -ano | findstr :5106

# Kill conflicting process
taskkill /PID <PID> /F
```

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Migration Errors

See [Database Setup Guide](database-setup.md#troubleshooting)

## 📊 Development Ports

| Service | Port | URL |
|---------|------|-----|
| API | 5292 | `http://localhost:5292` |
| Swagger | 5292 | `http://localhost:5292/swagger` |
| Client | 5106 | `http://localhost:5106` |
| PostgreSQL | 5432 | N/A (database only) |
| pgAdmin | 5050 | `http://localhost:5050` |

## 📚 Related Documentation

- [Quick Start Guide](quick-start.md) - Fast setup
- [VS Code Configuration](vscode-configuration.md) - Launch configs and debugging
- [Database Setup](database-setup.md) - PostgreSQL and EF Core
- [Docker Setup](docker-setup.md) - Docker Compose details
- [API Development](../development/api-development.md) - API guidelines
- [Client Development](../development/client-development.md) - Client development

## 🎉 You're All Set!

Your local development environment is fully configured. Press `F5` and start building!

Need help? Check the [Troubleshooting Guide](../reference/troubleshooting.md).


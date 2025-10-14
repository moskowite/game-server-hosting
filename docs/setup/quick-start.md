# Quick Start Guide

Get your Game Server Hosting development environment up and running in minutes!

## ⚡ Prerequisites

Before you begin, ensure you have:

- ✅ **Docker Desktop** installed and running
- ✅ **Git** installed
- ✅ **.NET 8 SDK** installed
- ✅ **VS Code or Cursor** installed

## 🚀 Quick Setup (5 Minutes)

### Step 1: Clone and Setup

```bash
# Clone the repository
git clone <repository-url>
cd game-server-hosting

# Restore NuGet packages
dotnet restore
```

### Step 2: Start PostgreSQL

```bash
# Start PostgreSQL in Docker
docker-compose up -d postgres

# Verify it's running
docker ps
```

### Step 3: Apply Database Migrations

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Apply migrations
dotnet ef database update --project src/GameServerHosting.Api
```

### Step 4: Launch the Application

**Option A: Using VS Code/Cursor (Recommended)**

1. Open the project in VS Code/Cursor
2. Press `F5`
3. Select "Full Stack (API + Client)"
4. Both API and Client will launch automatically!

**Option B: Command Line**

```bash
# Terminal 1 - API
dotnet run --project src/GameServerHosting.Api

# Terminal 2 - Client  
dotnet run --project src/GameServerHosting.Client
```

### Step 5: Verify Everything Works

- **API**: Open `http://localhost:5292/swagger`
- **Client**: Open `http://localhost:5106`
- **Database**: PostgreSQL running on `localhost:5432`

## 🎉 You're Ready!

Your development environment is now set up! Here's what happens automatically:

- ✅ PostgreSQL starts via Docker
- ✅ API runs on port 5292 with Swagger UI
- ✅ Blazor client runs on port 5106
- ✅ Hot reload is enabled for both projects

## 📋 What Was Configured

| Component | Status | Details |
|-----------|--------|---------|
| **PostgreSQL** | ✅ Running | Docker container on port 5432 |
| **API** | ✅ Running | ASP.NET Core on port 5292 |
| **Client** | ✅ Running | Blazor WASM on port 5106 |
| **Database** | ✅ Migrated | EF Core migrations applied |
| **VS Code** | ✅ Configured | Launch configs ready |

## 🎯 Next Steps

Now that you're set up, here are some things you can do:

### Explore the API

1. Open Swagger UI: `http://localhost:5292/swagger`
2. Try the authentication endpoints:
   - `POST /api/auth/register` - Create a new user
   - `POST /api/auth/login` - Get a JWT token
3. Use the token to access protected endpoints

### Explore the Client

1. Open the app: `http://localhost:5106`
2. Navigate through the pages
3. Check out the dashboard
4. Test the server catalog

### Start Developing

Choose what you want to work on:

- **Backend**: [API Development Guide](../development/api-development.md)
- **Frontend**: [Client Development Guide](../development/client-development.md)
- **Database**: [Database Schema Guide](../development/database-schema.md)

## ⚙️ Development Configuration

### PostgreSQL (Auto-starts)

- **Host**: localhost
- **Port**: 5432
- **Database**: gameserverhosting_dev
- **Username**: postgres
- **Password**: postgres

### API (ASP.NET Core)

- **Port**: 5292
- **Environment**: Development
- **Swagger**: `http://localhost:5292/swagger`
- **Hot Reload**: Enabled with `dotnet watch`

### Client (Blazor WebAssembly)

- **Port**: 5106
- **Environment**: Development  
- **API URL**: `http://localhost:5292` (configured automatically)
- **Hot Reload**: Enabled

## 🔧 Common Commands

```bash
# Start/Stop Database
docker-compose up -d postgres      # Start
docker-compose down                # Stop

# Run with Hot Reload
dotnet watch run --project src/GameServerHosting.Api
dotnet watch run --project src/GameServerHosting.Client

# Database Migrations
dotnet ef migrations add <Name> --project src/GameServerHosting.Api
dotnet ef database update --project src/GameServerHosting.Api

# Run Tests
dotnet test

# Build
dotnet build

# Clean
dotnet clean
```

## 🐛 Troubleshooting

### PostgreSQL Not Starting?

```bash
# Check Docker is running
docker ps

# If not, start Docker Desktop and retry
docker-compose up -d postgres
```

### Port Already in Use?

```bash
# Check what's using the port
netstat -ano | findstr :5292   # API
netstat -ano | findstr :5106   # Client

# Kill the process
taskkill /PID <PID> /F
```

### Database Migration Errors?

```bash
# Reset the database
dotnet ef database drop --project src/GameServerHosting.Api
dotnet ef database update --project src/GameServerHosting.Api
```

### Build Errors?

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## 📚 More Information

- **Complete Setup Guide**: [Local Development Setup](local-development.md)
- **VS Code Debugging**: [VS Code Configuration](vscode-configuration.md)
- **Database Details**: [Database Setup](database-setup.md)
- **Troubleshooting**: [Troubleshooting Guide](../reference/troubleshooting.md)

## 💡 Pro Tips

1. **Use the Full Stack launch config** in VS Code to run everything at once
2. **PostgreSQL auto-starts** when you launch the API via VS Code
3. **Hot reload is your friend** - changes apply without restarting
4. **Swagger is always available** at `/swagger` on the API
5. **Check the logs** if something doesn't work

---

**Ready to build something awesome?** 🚀

Head over to the [Development Guide](../development/architecture.md) to learn about the system architecture!


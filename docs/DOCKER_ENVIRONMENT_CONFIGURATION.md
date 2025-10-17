# Docker Environment Configuration Guide

## Overview
Both the API and Client projects now support dynamic environment-aware Docker builds that automatically use the correct configuration for Development vs Production.

## How It Works

### **API (Server-Side .NET Core)**

Unlike Blazor WASM, the API is a server-side application, so it can use:
- ✅ `ASPNETCORE_ENVIRONMENT` environment variable
- ✅ Environment variable overrides (e.g., `ConnectionStrings__DefaultConnection`)
- ✅ Dynamic appsettings file loading

#### **Configuration Hierarchy**
1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific overrides)
3. Environment variables (highest priority)

#### **Docker Implementation**
```dockerfile
# Accept build configuration
ARG BUILD_CONFIGURATION=Release
ENV BUILD_CONFIGURATION=${BUILD_CONFIGURATION}

# Build with configuration
RUN dotnet publish -c ${BUILD_CONFIGURATION} -o /app/publish

# Set environment based on build configuration
ENV ASPNETCORE_ENVIRONMENT=${BUILD_CONFIGURATION}
```

#### **Docker Compose Override**
```yaml
api:
  build:
    args:
      BUILD_CONFIGURATION: Development
  environment:
    # Override localhost with Docker container hostnames
    ConnectionStrings__DefaultConnection: "Host=postgres;Database=..."
    RabbitMQ__Host: "rabbitmq"
    EmailSettings__Smtp__Host: "maildev"
```

###**Client (Blazor WASM)**

Blazor WASM runs in the browser, so it uses:
- ✅ Build-time configuration file replacement
- ❌ Environment variables (not accessible in browser)
- ❌ Server-side environment detection

#### **Docker Implementation**
```dockerfile
# Build with configuration
RUN dotnet publish -c ${BUILD_CONFIGURATION} -o /app/publish

# Replace appsettings.json with environment-specific version
RUN if [ "${BUILD_CONFIGURATION}" = "Development" ]; then \
        cp appsettings.Development.json appsettings.json; \
    fi
```

## Configuration Files

### **API Configuration Structure**

#### `appsettings.json` (Production - Base)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gameserverhosting;..."
  },
  "RabbitMQ": {
    "Host": "localhost",
    ...
  }
}
```

#### `appsettings.Development.json` (Development - Local IDE)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gameserverhosting_dev;..."
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Debug" }
  }
}
```

#### **Docker Compose Override** (Development - Docker)
```yaml
environment:
  # These override the appsettings values for Docker networking
  ConnectionStrings__DefaultConnection: "Host=postgres;..."
  RabbitMQ__Host: "rabbitmq"
  EmailSettings__Smtp__Host: "maildev"
```

### **Client Configuration Structure**

#### `appsettings.json` (Production)
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.gameserverhosting.com"
  }
}
```

#### `appsettings.Development.json` (Development)
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5292/"
  }
}
```

## Usage Examples

### **Local Development (Docker Compose)**
```bash
# Automatically uses Development configuration
docker-compose up --build

# What happens:
# - API: Builds with Development, ASPNETCORE_ENVIRONMENT=Development
# - API: appsettings.Development.json loaded
# - API: Environment variables override Docker hostnames
# - Client: Builds with Development, copies appsettings.Development.json
```

### **Production Build**
```bash
# Build without override file (uses defaults)
docker-compose -f docker-compose.yml build

# What happens:
# - API: Builds with Release, ASPNETCORE_ENVIRONMENT=Release  
# - API: Uses appsettings.json
# - Client: Builds with Release, uses appsettings.json
```

### **Staging Build**
```bash
# Build for staging environment
docker build \
  --build-arg BUILD_CONFIGURATION=Staging \
  -f src/GameServerHosting.Api/Dockerfile \
  -t api:staging .
```

## Environment Variable Override Pattern

For .NET Core, the configuration system supports hierarchical environment variable overrides using `__` (double underscore) as a separator:

```yaml
# Override nested configuration
ConnectionStrings__DefaultConnection: "value"  # Overrides ConnectionStrings:DefaultConnection
Serilog__MinimumLevel__Default: "Debug"       # Overrides Serilog:MinimumLevel:Default
RabbitMQ__Host: "rabbitmq"                     # Overrides RabbitMQ:Host
EmailSettings__Smtp__Host: "maildev"           # Overrides EmailSettings:Smtp:Host
```

## Why This Approach?

### **Benefits**
✅ **Proper Separation**: Production settings in production files  
✅ **Docker-Friendly**: Container hostnames override localhost automatically  
✅ **IDE-Friendly**: Local debugging still uses localhost  
✅ **Secure**: Different secrets per environment  
✅ **Flexible**: Easy to add new environments (Staging, QA, etc.)  
✅ **Standards-Compliant**: Follows .NET Core configuration best practices  

### **API vs Client Differences**

| Aspect | API (Server-Side) | Client (Blazor WASM) |
|--------|-------------------|----------------------|
| **Environment Detection** | ✅ `ASPNETCORE_ENVIRONMENT` | ❌ Not available |
| **Env Var Overrides** | ✅ Yes | ❌ No (runs in browser) |
| **Configuration Loading** | Runtime (dynamic) | Build-time (static) |
| **Solution** | Environment variables | File replacement |

## Troubleshooting

### **API Connecting to Wrong Database**
**Symptom**: API tries to connect to `localhost` instead of `postgres` container  
**Cause**: Environment variable override not working  
**Fix**: Check `docker-compose.override.yml` has `ConnectionStrings__DefaultConnection` set

### **Client Connecting to Wrong API**
**Symptom**: Client shows loading spinner forever  
**Cause**: `appsettings.json` not replaced with Development version  
**Fix**: Verify Dockerfile copies `appsettings.Development.json` when `BUILD_CONFIGURATION=Development`

### **Migrations Not Running**
**Symptom**: `relation "outbox_messages" does not exist`  
**Cause**: Database hasn't been initialized  
**Fix**: API now calls `EnsureDatabaseMigratedAsync()` on startup (already implemented in Program.cs)

## Verification

### **Check API Configuration**
```bash
# Check environment inside container
docker exec gameserver-api printenv ASPNETCORE_ENVIRONMENT

# Should show: Development (in docker-compose)
# Should show: Production (in production builds)
```

### **Check Client Configuration**
```bash
# Check appsettings in container
docker exec gameserver-client cat /usr/share/nginx/html/appsettings.json

# Should show: http://localhost:5292/ (in Development)
# Should show: https://api.gameserverhosting.com (in Production)
```

## Adding New Environments

### **For API** (e.g., Staging)
1. Create `appsettings.Staging.json`
2. Build with `BUILD_CONFIGURATION=Staging`
3. ASPNETCORE_ENVIRONMENT will automatically be set to `Staging`

### **For Client** (e.g., Staging)
1. Create `appsettings.Staging.json`
2. Update Dockerfile to handle Staging:
   ```dockerfile
   RUN if [ "${BUILD_CONFIGURATION}" = "Staging" ]; then \
       cp appsettings.Staging.json appsettings.json; \
   fi
   ```
3. Build with `BUILD_CONFIGURATION=Staging`

---

**Implementation Date**: October 16, 2025  
**Status**: ✅ COMPLETE  
**Impact**: Proper environment configuration for all services


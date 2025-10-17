# Environment Configuration Solution - Blazor WASM

## Problem Statement
The Blazor WebAssembly application was stuck at the loading screen because it was trying to connect to a production API URL (`https://api.gameserverhosting.com`) instead of the local development API (`http://localhost:5292/`).

## Why Standard Environment Variables Don't Work

Unlike server-side ASP.NET Core applications, **Blazor WebAssembly runs entirely in the browser**. This means:
- ❌ Server environment variables are not accessible
- ❌ `ASPNETCORE_ENVIRONMENT` doesn't affect the browser
- ❌ Configuration files are static assets served to the client
- ✅ Configuration must be baked into the build

## Solution Implemented

### 1. **Proper Configuration File Structure**

#### `appsettings.json` (Production - Default)
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.gameserverhosting.com"
  },
  "Stripe": {
    "PublishableKey": "pk_live_YOUR_PRODUCTION_KEY"
  }
}
```

#### `appsettings.Development.json` (Development - Local)
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5292/"
  },
  "Stripe": {
    "PublishableKey": "pk_test_51QnCO0A6FPCehEZz..."
  }
}
```

### 2. **Dynamic Dockerfile with Build Arguments**

Updated the Dockerfile to accept a `BUILD_CONFIGURATION` argument:

```dockerfile
# Accept build configuration as argument (defaults to Release for production)
ARG BUILD_CONFIGURATION=Release
ENV BUILD_CONFIGURATION=${BUILD_CONFIGURATION}

# Build with dynamic configuration
RUN dotnet publish GameServerHosting.Client.csproj \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish

# Copy environment-specific appsettings over the default one
RUN if [ "${BUILD_CONFIGURATION}" = "Development" ]; then \
        echo "Using Development configuration..."; \
        cp /app/publish/wwwroot/appsettings.Development.json /app/publish/wwwroot/appsettings.json; \
    fi
```

### 3. **Docker Compose Override for Development**

Updated `docker-compose.override.yml` to pass the build argument:

```yaml
client:
  build:
    context: .
    dockerfile: src/GameServerHosting.Client/Dockerfile
    args:
      # Use Development configuration to load appsettings.Development.json
      BUILD_CONFIGURATION: Development
  ports:
    - "5173:80"
```

## How It Works

### Development Build (docker-compose)
1. Docker Compose passes `BUILD_CONFIGURATION: Development`
2. Dockerfile builds with `-c Development`
3. Dockerfile copies `appsettings.Development.json` → `appsettings.json`
4. Container serves Blazor app with local API URL (`http://localhost:5292/`)

### Production Build (CI/CD or manual)
1. No build argument passed (defaults to `Release`)
2. Dockerfile builds with `-c Release`
3. `appsettings.json` remains unchanged with production URL
4. Container serves Blazor app with production API URL

## Benefits

✅ **Proper Separation of Concerns**
- Development settings stay in Development files
- Production settings stay in Production files
- No hardcoded values

✅ **Environment-Aware Builds**
- Single Dockerfile works for all environments
- Configuration is automatic based on build context
- No manual file editing required

✅ **Best Practices**
- Follows .NET configuration patterns
- Secure secrets management (different keys per environment)
- Easy to extend for Staging, QA, etc.

✅ **Developer Experience**
- `docker-compose up` just works for local development
- Production builds use correct settings automatically
- Clear separation makes debugging easier

## Usage Examples

### Local Development
```bash
# Automatically uses Development configuration
docker-compose up --build client

# Or without rebuild
docker-compose up client
```

### Production Build
```bash
# Manually build for production
docker build -f src/GameServerHosting.Client/Dockerfile \
  --build-arg BUILD_CONFIGURATION=Release \
  -t client:production .
```

### Staging Build
```bash
# First create appsettings.Staging.json, then:
docker build -f src/GameServerHosting.Client/Dockerfile \
  --build-arg BUILD_CONFIGURATION=Staging \
  -t client:staging .
```

## Adding New Environments

To add a new environment (e.g., Staging):

1. **Create configuration file**:
   ```bash
   # src/GameServerHosting.Client/wwwroot/appsettings.Staging.json
   {
     "ApiSettings": {
       "BaseUrl": "https://staging-api.gameserverhosting.com"
     },
     "Stripe": {
       "PublishableKey": "pk_test_staging_key"
     }
   }
   ```

2. **Update Dockerfile** (add Staging to the copy logic):
   ```dockerfile
   RUN if [ "${BUILD_CONFIGURATION}" = "Development" ]; then \
           cp /app/publish/wwwroot/appsettings.Development.json /app/publish/wwwroot/appsettings.json; \
       elif [ "${BUILD_CONFIGURATION}" = "Staging" ]; then \
           cp /app/publish/wwwroot/appsettings.Staging.json /app/publish/wwwroot/appsettings.json; \
       fi
   ```

3. **Build with Staging configuration**:
   ```bash
   docker build --build-arg BUILD_CONFIGURATION=Staging ...
   ```

## Troubleshooting

### App Still Loads Production URL
**Cause**: Container wasn't rebuilt or old image is cached  
**Fix**: `docker-compose build --no-cache client && docker-compose up -d client`

### Configuration Not Changing
**Cause**: Browser caching the old appsettings.json  
**Fix**: Hard refresh in browser (Ctrl+Shift+R or Ctrl+F5)

### Wrong Environment in CI
**Cause**: CI is using default Release configuration  
**Fix**: Add `--build-arg BUILD_CONFIGURATION=Development` to build command

## Verification

To verify which configuration is active:

```bash
# Check configuration in running container
docker exec gameserver-client cat /usr/share/nginx/html/appsettings.json

# Should show http://localhost:5292/ for Development
# Should show https://api.gameserverhosting.com for Production
```

## Security Considerations

✅ **Separate API Keys**
- Development uses test Stripe keys (`pk_test_...`)
- Production uses live Stripe keys (`pk_live_...`)

✅ **No Secrets in Source**
- Placeholder keys in git
- Real keys injected during deployment
- Environment-specific key management

✅ **Clear Separation**
- Development config never used in production
- Production config never used in development
- Build-time validation possible

---

**Implementation Date**: October 16, 2025  
**Status**: ✅ COMPLETE and TESTED  
**Impact**: Proper environment configuration management for Blazor WASM


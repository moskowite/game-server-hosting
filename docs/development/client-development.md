# Client Configuration Guide

This document explains the environment-based configuration system for the Game Server Hosting Client application.

## Overview

The Client uses a flexible, environment-based configuration system that eliminates hardcoded URLs and allows seamless deployment across different environments (Development, Staging, Production).

## Configuration Files

### Location
All configuration files are located in `wwwroot/`:

```
wwwroot/
├── appsettings.json                    # Production settings (default)
├── appsettings.Development.json        # Local development settings
└── appsettings.Staging.json            # Staging environment settings
```

### Environment Selection

The appropriate configuration file is automatically loaded based on the `ASPNETCORE_ENVIRONMENT` environment variable:

- `Development` → loads `appsettings.Development.json`
- `Staging` → loads `appsettings.Staging.json`
- `Production` (or any other value) → loads `appsettings.json`

## API Settings

### Current Configuration

**Development** (`appsettings.Development.json`):
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5292"
  }
}
```

**Staging** (`appsettings.Staging.json`):
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api-staging.gameserverhosting.com"
  }
}
```

**Production** (`appsettings.json`):
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.gameserverhosting.com"
  }
}
```

### How It Works

The `Program.cs` reads the configuration and configures the HttpClient:

```csharp
// Load API settings from environment-specific config
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() 
    ?? new ApiSettings { BaseUrl = builder.HostEnvironment.BaseAddress };

// Configure HttpClient with the dynamic base URL
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiSettings.BaseUrl) 
});
```

### Fallback Behavior

If the `ApiSettings` section is missing or the configuration file is not found, the system falls back to `builder.HostEnvironment.BaseAddress` (the Client's own URL).

## Adding New Settings

To add new configuration settings:

1. **Create the model** in `Models/`:
```csharp
public class MySettings
{
    public string SomeValue { get; set; } = string.Empty;
}
```

2. **Add to configuration files**:
```json
{
  "ApiSettings": { ... },
  "MySettings": {
    "SomeValue": "environment-specific-value"
  }
}
```

3. **Load in Program.cs**:
```csharp
var mySettings = builder.Configuration.GetSection("MySettings").Get<MySettings>();
builder.Services.AddSingleton(mySettings);
```

4. **Inject in components/services**:
```csharp
@inject MySettings Settings

// Use: Settings.SomeValue
```

## Best Practices

### ✅ DO:
- Use environment-specific configuration files for different API URLs
- Store non-sensitive, environment-specific settings in these files
- Use meaningful setting names and organize them in sections
- Provide fallback values for optional settings

### ❌ DON'T:
- Store secrets or sensitive data in these files (they're included in the published app)
- Hardcode URLs or environment-specific values in code
- Mix concerns in a single settings object

## Environment Variables

You can override configuration values using environment variables in production:

```bash
# Example: Override API URL at runtime (if supported by hosting platform)
export ApiSettings__BaseUrl=https://api.custom-domain.com
```

Note: Double underscores (`__`) represent nested configuration levels.

## Local Development

When running locally via:
- **VS Code Launch Configurations**: Automatically uses `Development` environment
- **Command Line**: Set environment explicitly:
  ```bash
  $env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run
  ```

## Deployment Considerations

### During Build/Publish

The Blazor WebAssembly app is built with the environment specified at build time:

```bash
# Build for Production (default)
dotnet publish -c Release

# Build for Staging
dotnet publish -c Release /p:EnvironmentName=Staging
```

### Static File Hosting

Since Blazor WASM apps are static files, the environment is "baked in" at build time. For runtime configuration changes, consider:

1. **Build separate artifacts per environment**
2. **Use a configuration endpoint** that the app calls on startup
3. **Replace values during deployment** using CI/CD scripts

## Testing Configuration

To verify which configuration is loaded:

1. **Add logging in Program.cs**:
```csharp
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>();
Console.WriteLine($"API Base URL: {apiSettings?.BaseUrl}");
```

2. **Check browser console** for the output when the app loads

3. **Inspect network requests** in browser DevTools to verify API calls go to correct URL

## Troubleshooting

### Issue: Client can't reach API
- Verify correct `appsettings.[Environment].json` is being loaded
- Check `ASPNETCORE_ENVIRONMENT` value
- Ensure API URL in config matches running API instance
- Check browser console for CORS errors

### Issue: Wrong environment config loaded
- Confirm environment variable is set correctly
- Rebuild the app (config is loaded at build time)
- Clear browser cache

### Issue: Configuration not updating
- Remember: Configuration is embedded at build time
- Must rebuild the app after changing config files
- Run `dotnet clean` then `dotnet build` if changes aren't reflected

## Security Notes

⚠️ **Important**: All configuration files in `wwwroot/` are included in the published application and accessible to users. Never store:
- API keys
- Secrets
- Passwords
- Private tokens

For sensitive configuration, use:
- Environment variables on the server
- Azure Key Vault / AWS Secrets Manager
- Backend API endpoints that provide configuration

---

**Last Updated**: Following implementation of environment-based configuration system
**Related Files**: `Program.cs`, `Models/ApiSettings.cs`, `wwwroot/appsettings*.json`


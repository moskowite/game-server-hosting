# Client API Configuration - Quick Start

## What Changed?

The Client now uses **environment-based configuration** instead of hardcoded API URLs. This means:
- ✅ Same codebase works in all environments
- ✅ No code changes needed for deployment
- ✅ Easy to maintain and update URLs

## File Structure

```
src/GameServerHosting.Client/
├── wwwroot/
│   ├── appsettings.json                   # Production API URL
│   ├── appsettings.Development.json       # Local dev API URL (localhost:5292)
│   └── appsettings.Staging.json           # Staging API URL
├── Models/
│   └── ApiSettings.cs                     # API configuration model
└── Program.cs                             # Reads config and sets up HttpClient
```

## How It Works

1. **Configuration files** define the API URL per environment:
   ```json
   {
     "ApiSettings": {
       "BaseUrl": "http://localhost:5292"
     }
   }
   ```

2. **Program.cs** loads the appropriate config:
   ```csharp
   var apiSettings = builder.Configuration
       .GetSection("ApiSettings")
       .Get<ApiSettings>();
   
   builder.Services.AddScoped(sp => new HttpClient 
   { 
       BaseAddress = new Uri(apiSettings.BaseUrl) 
   });
   ```

3. **Blazor automatically** selects the right file based on `ASPNETCORE_ENVIRONMENT`

## Current Settings

| Environment | API URL |
|------------|---------|
| **Development** | `http://localhost:5292` |
| **Staging** | `https://api-staging.gameserverhosting.com` |
| **Production** | `https://api.gameserverhosting.com` |

## Local Development

When using VS Code launch configurations:
- Environment is automatically set to `Development`
- Client calls API at `http://localhost:5292`
- No manual configuration needed! 🎉

## Updating API URLs

To change the API URL for any environment:

1. Edit the appropriate `appsettings.[Environment].json` file
2. Update the `BaseUrl` value
3. Rebuild the Client application
4. Done!

Example:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://my-new-api-url.com"
  }
}
```

## Benefits

### For Developers
- Local development "just works" with localhost
- No environment-specific code branches
- Easy to test against different API instances

### For DevOps
- Single build artifact per environment
- Configuration changes don't require code changes
- Clear separation of config from code

### For Team
- Less confusion about which URL to use
- Consistent approach across all environments
- Self-documenting configuration

## Troubleshooting

**Client not connecting to API?**
1. Check which environment is active: Look for `ASPNETCORE_ENVIRONMENT`
2. Verify the corresponding `appsettings.[Environment].json` has correct URL
3. Rebuild the Client app (config is loaded at build time)
4. Check browser DevTools → Network tab to see actual request URLs

**Need to override temporarily?**
Edit the appropriate `appsettings.[Environment].json` file and rebuild.

## Related Documentation

- **Full Configuration Guide**: [`src/GameServerHosting.Client/CONFIGURATION.md`](src/GameServerHosting.Client/CONFIGURATION.md)
- **VS Code Launch Setup**: [`.vscode/README.md`](.vscode/README.md)
- **API Documentation**: See API project for backend setup

---

**Key Takeaway**: The API URL is now configured through environment-specific JSON files, eliminating hardcoded values and enabling seamless deployment across all environments.


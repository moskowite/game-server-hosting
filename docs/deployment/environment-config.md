# Environment Configuration Guide

Guide for configuring the Game Server Hosting platform across different environments.

## 🌍 Environments

The platform supports three environments:

- **Development** - Local development
- **Staging** - Pre-production testing
- **Production** - Live production environment

## ⚙️ Configuration Structure

### API Configuration Files

```
src/GameServerHosting.Api/
├── appsettings.json                    # Production (default)
├── appsettings.Development.json        # Local development
└── appsettings.Staging.json           # Staging environment
```

### Client Configuration Files

```
src/GameServerHosting.Client/wwwroot/
├── appsettings.json                    # Production
├── appsettings.Development.json        # Local development
└── appsettings.Staging.json           # Staging environment
```

## 🔧 API Configuration

### Development (appsettings.Development.json)

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
      "http://localhost:5106"
    ]
  },
  "Swagger": {
    "Enabled": true
  }
}
```

### Staging (appsettings.Staging.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=staging-db.example.com;Database=gameserverhosting_staging;Username=app_user;Password=${DB_PASSWORD}"
  },
  "JwtSettings": {
    "SecretKey": "${JWT_SECRET}",
    "ExpirationInMinutes": 60
  },
  "EmailSettings": {
    "Provider": "SendGrid",
    "FromEmail": "staging@gameserverhosting.com",
    "SendGrid": {
      "ApiKey": "${SENDGRID_API_KEY}"
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "https://staging.gameserverhosting.com"
    ]
  },
  "Swagger": {
    "Enabled": true
  }
}
```

### Production (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "${CONNECTION_STRING}"
  },
  "JwtSettings": {
    "SecretKey": "${JWT_SECRET}",
    "ExpirationInMinutes": 60
  },
  "EmailSettings": {
    "Provider": "SendGrid",
    "FromEmail": "noreply@gameserverhosting.com",
    "SendGrid": {
      "ApiKey": "${SENDGRID_API_KEY}"
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "https://gameserverhosting.com",
      "https://www.gameserverhosting.com"
    ]
  },
  "Swagger": {
    "Enabled": false  // Disabled in production
  }
}
```

## 🌐 Client Configuration

### Development

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5292"
  }
}
```

### Staging

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api-staging.gameserverhosting.com"
  }
}
```

### Production

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.gameserverhosting.com"
  }
}
```

## 🔒 Secrets Management

### Development

Use User Secrets for sensitive data:

```bash
# Initialize user secrets
dotnet user-secrets init --project src/GameServerHosting.Api

# Set a secret
dotnet user-secrets set "EmailSettings:SendGrid:ApiKey" "YOUR_API_KEY" --project src/GameServerHosting.Api
```

### Staging & Production

Use environment variables or Azure Key Vault:

**Environment Variables:**
```bash
export ConnectionStrings__DefaultConnection="Host=...;Database=...;..."
export JwtSettings__SecretKey="your-secret-key"
export EmailSettings__SendGrid__ApiKey="your-api-key"
```

**Azure Key Vault:**
```csharp
// In Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

## 🚀 Deployment Configuration

### Building for Different Environments

```bash
# Development (default)
dotnet build

# Staging
dotnet publish -c Release /p:EnvironmentName=Staging

# Production
dotnet publish -c Release /p:EnvironmentName=Production
```

### Setting Environment Variable

**Development (VS Code):**
- Automatically set to `Development` via launch configurations

**Staging/Production:**
```bash
# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Staging

# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT="Staging"

# Windows CMD
set ASPNETCORE_ENVIRONMENT=Staging
```

## 📋 Configuration Checklist

### Development
- [ ] PostgreSQL connection string (localhost)
- [ ] Development JWT secret
- [ ] SMTP settings for local email testing
- [ ] CORS allows localhost:5106
- [ ] Swagger enabled

### Staging
- [ ] Staging database connection
- [ ] Staging JWT secret (from Key Vault)
- [ ] SendGrid API key
- [ ] CORS allows staging domain
- [ ] Swagger enabled for testing

### Production
- [ ] Production database connection (from Key Vault)
- [ ] Strong JWT secret (from Key Vault)
- [ ] SendGrid API key (from Key Vault)
- [ ] CORS allows production domains only
- [ ] Swagger disabled
- [ ] HTTPS enforced
- [ ] Security headers configured

## 🛡️ Security Recommendations

1. **Never commit secrets** to source control
2. **Use strong secrets** in production (32+ characters)
3. **Rotate secrets** regularly
4. **Use Azure Key Vault** or similar for production
5. **Enable HTTPS** in all environments except local dev
6. **Disable Swagger** in production
7. **Use read-only database users** where appropriate
8. **Implement rate limiting** for public endpoints

## 📚 Related Documentation

- [Deployment Guide](deployment-guide.md) - Deployment steps
- [CI/CD Pipeline](ci-cd.md) - Automated deployment
- [Quick Start](../setup/quick-start.md) - Local setup

---

**Remember**: Configuration should be environment-specific, never hardcoded! 🔒


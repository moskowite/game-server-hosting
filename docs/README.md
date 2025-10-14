# Game Server Hosting - Documentation

Welcome to the Game Server Hosting platform documentation! This guide will help you get started, develop features, and deploy the application.

## 📚 Documentation Index

### 🚀 Getting Started

**New to the project? Start here:**

1. **[Quick Start Guide](setup/quick-start.md)** - Get up and running in minutes
2. **[Local Development Setup](setup/local-development.md)** - Complete setup guide for local development
3. **[VS Code Configuration](setup/vscode-configuration.md)** - Debugging and launch configurations
4. **[Database Setup](setup/database-setup.md)** - PostgreSQL and migrations guide
5. **[Docker Setup](setup/docker-setup.md)** - Docker Compose configuration

### 💻 Development

**Building features? Look here:**

1. **[Technical Architecture](development/architecture.md)** - System design and architecture
2. **[Database Schema](development/database-schema.md)** - Database structure and relationships
3. **[API Development](development/api-development.md)** - API guidelines and patterns
4. **[Client Development](development/client-development.md)** - Blazor client configuration and development

### ✨ Features

**Feature-specific documentation:**

1. **[Authentication & Authorization](features/authentication.md)** - JWT, Identity, and security
2. **[Email Verification](features/email-verification.md)** - Email system and verification flow
3. **[Dashboard](features/dashboard.md)** - User dashboard implementation
4. **[User Management](features/user-management.md)** - Profiles, settings, and preferences

### 🚢 Deployment

**Ready to deploy?**

1. **[Deployment Guide](deployment/deployment-guide.md)** - Step-by-step deployment instructions
2. **[CI/CD Pipeline](deployment/ci-cd.md)** - GitHub Actions and automated workflows
3. **[Environment Configuration](deployment/environment-config.md)** - Environment-specific settings

### 📖 Reference

**Additional resources:**

1. **[Migration Guide](reference/migration-guide.md)** - Database migration reference
2. **[EF Core Setup](reference/ef-core-setup.md)** - Entity Framework Core guide
3. **[Troubleshooting](reference/troubleshooting.md)** - Common issues and solutions
4. **[Project Roadmap](reference/roadmap.md)** - Future plans and features

## 🎯 Quick Links

### For Developers

- **First time setup?** → [Quick Start Guide](setup/quick-start.md)
- **Need to debug?** → [VS Code Configuration](setup/vscode-configuration.md)
- **Database issues?** → [Troubleshooting](reference/troubleshooting.md)
- **API reference?** → Run the app and visit `http://localhost:5292/swagger`

### For DevOps

- **Deploying?** → [Deployment Guide](deployment/deployment-guide.md)
- **CI/CD setup?** → [CI/CD Pipeline](deployment/ci-cd.md)
- **Environment config?** → [Environment Configuration](deployment/environment-config.md)

### For Contributors

- **Architecture overview?** → [Technical Architecture](development/architecture.md)
- **Database schema?** → [Database Schema](development/database-schema.md)
- **Coding standards?** → [API Development](development/api-development.md)

## 🏗️ Project Structure

```
game-server-hosting/
├── src/
│   ├── GameServerHosting.Api/          # ASP.NET Core Web API
│   ├── GameServerHosting.Client/       # Blazor WebAssembly Client
│   └── GameServerHosting.Shared/       # Shared models and DTOs
├── docs/                                # This documentation
├── .vscode/                             # VS Code launch configs
├── docker-compose.yml                   # Docker services
└── README.md                            # Project overview
```

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core 8.0, Entity Framework Core, PostgreSQL
- **Frontend**: Blazor WebAssembly, MudBlazor
- **Authentication**: ASP.NET Identity, JWT
- **Email**: SMTP with MailKit
- **Deployment**: Docker, GitHub Actions
- **Database**: PostgreSQL with EF Core migrations

## 📞 Getting Help

1. **Documentation**: Start with the relevant guide above
2. **API Reference**: Run the app and check Swagger UI
3. **Troubleshooting**: Check the [Troubleshooting Guide](reference/troubleshooting.md)
4. **Issues**: Check GitHub Issues or create a new one

## 🚀 Quick Commands

```bash
# Start development environment
docker-compose up -d postgres
dotnet run --project src/GameServerHosting.Api
dotnet run --project src/GameServerHosting.Client

# Run migrations
dotnet ef database update --project src/GameServerHosting.Api

# Run tests
dotnet test

# Build for production
dotnet publish -c Release
```

## 📝 Documentation Maintenance

This documentation is organized to be:
- **Easy to navigate**: Clear hierarchy and index
- **Up to date**: Updated with code changes
- **Comprehensive**: Covers setup, development, and deployment
- **Searchable**: Well-structured with clear headings

**Note**: When adding new features, please update the relevant documentation.

---

**Last Updated**: October 2025
**Documentation Version**: 2.0

Happy coding! 🎉


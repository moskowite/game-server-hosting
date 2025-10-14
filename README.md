# Game Server Hosting Platform

[![CI Pipeline](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml)
[![CD Pipeline](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/cd.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/cd.yml)

A modern .NET 8.0 game server hosting marketplace with Blazor WebAssembly frontend, ASP.NET Core backend, and PostgreSQL database.

## 📚 Documentation

**👉 [Complete Documentation](docs/README.md)** - Start here for setup, development, and deployment guides

### Quick Links
- 🚀 [Quick Start](docs/setup/quick-start.md) - Get running in 5 minutes
- 💻 [Local Development](docs/setup/quick-start.md) - Complete setup guide
- 🏗️ [Architecture](docs/README.md#-project-structure) - System design
- 🐛 [Troubleshooting](docs/reference/troubleshooting.md) - Common issues

## Solution Structure

```
GameServerHosting/
├── src/                                    # Source projects
│   ├── GameServerHosting.Api/             # ASP.NET Core Web API
│   ├── GameServerHosting.Client/          # Blazor WebAssembly client
│   └── GameServerHosting.Shared/          # Shared models and contracts
├── tests/                                  # Test projects
├── docs/                                   # Documentation
└── GameServerHosting.sln                  # Solution file
```

## Projects

### GameServerHosting.Api
ASP.NET Core Web API project that provides the backend services for the game server hosting platform.

**Technology Stack:**
- .NET 8.0
- ASP.NET Core Web API
- Minimal APIs

### GameServerHosting.Client
Blazor WebAssembly client application for the user interface.

**Technology Stack:**
- .NET 8.0
- Blazor WebAssembly
- Bootstrap 5

### GameServerHosting.Shared
Shared class library containing common models, DTOs, and contracts used by both the API and Client projects.

**Technology Stack:**
- .NET 8.0
- Class Library

## 🚀 Quick Start

```bash
# 1. Start PostgreSQL
docker-compose up -d postgres

# 2. Apply migrations
dotnet ef database update --project src/GameServerHosting.Api

# 3. Run the application (in VS Code/Cursor)
Press F5 → Select "Full Stack (API + Client)"

# Or run from command line:
dotnet run --project src/GameServerHosting.Api      # Terminal 1
dotnet run --project src/GameServerHosting.Client   # Terminal 2
```

**Access the application:**
- 🔧 API/Swagger: http://localhost:5292/swagger
- 🌐 Client UI: http://localhost:5106
- 🗄️ pgAdmin: http://localhost:5050 (optional)

📖 **Need more details?** See the [Quick Start Guide](docs/setup/quick-start.md)

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| **Backend** | ASP.NET Core 8.0, Entity Framework Core |
| **Frontend** | Blazor WebAssembly |
| **Database** | PostgreSQL 16 |
| **Authentication** | ASP.NET Identity, JWT |
| **Email** | SMTP with MailKit |
| **Deployment** | Docker, GitHub Actions |

## 📋 Common Commands

```bash
# Development
dotnet build                                        # Build solution
dotnet test                                         # Run tests
dotnet watch run --project src/GameServerHosting.Api  # Hot reload

# Database
dotnet ef migrations add <Name> --project src/GameServerHosting.Api
dotnet ef database update --project src/GameServerHosting.Api

# Docker
docker-compose up -d              # Start all services
docker-compose down               # Stop all services  
docker ps                         # Check running containers

# CI/CD
gh workflow run ci.yml            # Trigger CI
gh workflow run cd.yml -f environment=staging    # Deploy to staging
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

See [Contributing Guide](docs/README.md) for more details.

## License

[Add your license here]

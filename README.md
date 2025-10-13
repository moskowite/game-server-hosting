# Game Server Hosting Platform

[![CI Pipeline](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml)
[![CD Pipeline](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/cd.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/cd.yml)

A .NET 8.0 solution for game server hosting marketplace.

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

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Your favorite IDE (Visual Studio, VS Code, JetBrains Rider)

### Building the Solution

```bash
dotnet build
```

### Running the API

```bash
dotnet run --project src/GameServerHosting.Api
```

The API will be available at `http://localhost:5000` by default.

### Running the Blazor Client

```bash
dotnet run --project src/GameServerHosting.Client
```

The client will be available at `http://localhost:5001` by default.

### Running Both Projects

To run both the API and Client simultaneously, open two terminal windows and run each project in its own terminal.

## Project References

- `GameServerHosting.Api` → references → `GameServerHosting.Shared`
- `GameServerHosting.Client` → references → `GameServerHosting.Shared`

## CI/CD Pipeline

This project uses GitHub Actions for continuous integration and deployment.

### Workflows

- **CI Pipeline**: Automated builds, tests, code quality checks, and security scans on every push and PR
- **CD Pipeline**: Automated deployments to staging and production environments
- **PR Checks**: Fast feedback on pull requests with validation and conflict detection
- **Dependabot**: Automated dependency updates for NuGet packages and GitHub Actions

For detailed CI/CD setup and usage instructions, see [CI/CD Setup Guide](CI_CD_SETUP.md).

### Quick Commands

```bash
# View workflow runs
gh run list

# Trigger CI manually
gh workflow run ci.yml

# Deploy to staging
gh workflow run cd.yml -f environment=staging

# Deploy to production
gh workflow run cd.yml -f environment=production
```

## Development

### Adding New Projects

To add a new project to the solution:

```bash
dotnet new <template> -n <ProjectName> -o src/<ProjectName>
dotnet sln add src/<ProjectName>/<ProjectName>.csproj
```

### Adding Project References

```bash
dotnet add <project-path> reference <reference-project-path>
```

## License

[Add your license here]

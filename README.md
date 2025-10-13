# Game Server Hosting Platform

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

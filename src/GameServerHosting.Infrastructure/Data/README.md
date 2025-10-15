# Database & Entity Framework Core Setup

This directory contains the database infrastructure for the Game Server Hosting platform using Entity Framework Core and PostgreSQL.

## 📁 Structure

```
Data/
├── GameServerDbContext.cs          # Main DbContext class
├── DesignTimeDbContextFactory.cs   # Factory for EF Core tools
├── MIGRATIONS.md                   # Detailed migrations guide
├── README.md                       # This file
├── Extensions/
│   └── DatabaseExtensions.cs       # Database utility extensions
└── Migrations/                     # Generated migrations (auto-created)
```

## 🚀 Quick Start

### 1. Install EF Core Tools (if not already installed)

```bash
dotnet tool install --global dotnet-ef
```

### 2. Configure Database Connection

Update the connection string in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gameserverhosting_dev;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  }
}
```

### 3. Create Your First Entity

Create entity classes in the appropriate domain folder, for example:

```csharp
public class GameServer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 4. Add DbSet to GameServerDbContext

```csharp
public class GameServerDbContext : DbContext
{
    // ... constructor ...
    
    public DbSet<GameServer> GameServers { get; set; }
    
    // ... OnModelCreating ...
}
```

### 5. Create and Apply Migration

```bash
cd src/GameServerHosting.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 🔧 What's Configured

### ✅ Packages Installed
- **Microsoft.EntityFrameworkCore** (8.0.20) - Core EF functionality
- **Microsoft.EntityFrameworkCore.Design** (8.0.20) - Design-time tools
- **Npgsql.EntityFrameworkCore.PostgreSQL** (8.0.11) - PostgreSQL provider

### ✅ Connection Strings
- **Production**: `appsettings.json` - Database: `gameserverhosting`
- **Development**: `appsettings.Development.json` - Database: `gameserverhosting_dev`

### ✅ DbContext Registration
The `GameServerDbContext` is registered in `Program.cs` with dependency injection:

```csharp
builder.Services.AddDbContext<GameServerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### ✅ Logging
EF Core SQL command logging is enabled in development for debugging.

## 📝 Next Steps

1. **Define Your Domain Entities**: Create your entity models (GameServer, User, etc.)
2. **Configure Relationships**: Use Fluent API in `OnModelCreating` to define relationships
3. **Create Initial Migration**: Run `dotnet ef migrations add InitialCreate`
4. **Apply Migration**: Run `dotnet ef database update`
5. **Add Repositories**: Create repository pattern for data access (optional but recommended)
6. **Seed Data**: Implement seed data in `DatabaseExtensions.SeedDatabaseAsync`

## 🛠️ Utilities

### DatabaseExtensions

Located in `Extensions/DatabaseExtensions.cs`, provides:

- **EnsureDatabaseMigratedAsync**: Automatically applies pending migrations
- **SeedDatabaseAsync**: Seeds initial data

Example usage in `Program.cs`:

```csharp
// After app.Build() but before app.Run()
if (app.Environment.IsDevelopment())
{
    await app.Services.EnsureDatabaseMigratedAsync();
    await app.Services.SeedDatabaseAsync();
}
```

## 📚 Resources

- [EF Core Documentation](https://docs.microsoft.com/ef/core/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- **Detailed Migration Guide**: See `MIGRATIONS.md` in this directory

## ⚠️ Important Notes

- Always review generated migrations before applying them
- Use descriptive migration names
- Never modify applied migrations - create new ones instead
- Keep connection strings secure (use User Secrets or environment variables in production)
- Consider using migration bundles for production deployments

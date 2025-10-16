# Database Setup Guide

Complete guide for setting up PostgreSQL, Entity Framework Core, and database migrations for the Game Server Hosting platform.

> 📚 **For detailed migration workflows**, see the [Database Migrations Guide](../development/database-migrations.md)

## 🚀 Quick Start

### Linux/macOS (Using Makefile)

```bash
# 1. Start PostgreSQL (Docker)
make docker.db.up

# 2. Install EF Core tools
dotnet tool install --global dotnet-ef

# 3. Apply migrations
make db.migrate
```

### Windows (Using PowerShell)

```powershell
# 1. Start PostgreSQL (Docker)
docker compose up -d postgres

# 2. Install EF Core tools
dotnet tool install --global dotnet-ef

# 3. Apply migrations
.\scripts\db-migrations.ps1 -Command migrate
```

## 🐳 PostgreSQL Setup

### Docker Compose (Recommended)

The project includes a `docker-compose.yml` with PostgreSQL pre-configured:

```yaml
services:
  postgres:
    image: postgres:16-alpine
    container_name: gameserver-postgres
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: gameserverhosting_dev
    volumes:
      - postgres_data:/var/lib/postgresql/data
```

**Start PostgreSQL:**
```bash
docker-compose up -d postgres
```

**Verify it's running:**
```bash
docker ps | grep gameserver-postgres
```

**Access psql:**
```bash
docker exec -it gameserver-postgres psql -U postgres -d gameserverhosting_dev
```

### Optional: pgAdmin

For a database GUI, start pgAdmin:

```bash
docker-compose up -d pgadmin
```

Access at `http://localhost:5050`:
- Email: `admin@gameserver.local`
- Password: `admin`

## 🗃️ Database Configuration

### Connection Strings

**Development** (`appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gameserverhosting_dev;Username=postgres;Password=postgres"
  }
}
```

**Production** (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gameserverhosting;Username=postgres;Password=postgres"
  }
}
```

⚠️ **Production**: Use environment variables or secrets management, not hardcoded passwords!

### DbContext Registration

The `GameServerDbContext` is registered in `Program.cs`:

```csharp
builder.Services.AddDbContext<GameServerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

## 🔧 Entity Framework Core

### Packages Installed

- `Microsoft.EntityFrameworkCore` (8.0.20)
- `Microsoft.EntityFrameworkCore.Design` (8.0.20)  
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0.11)

### DbContext Structure

```csharp
public class GameServerDbContext : IdentityDbContext<ApplicationUser>
{
    public GameServerDbContext(DbContextOptions<GameServerDbContext> options)
        : base(options) { }

    // DbSets
    public DbSet<ServerType> ServerTypes { get; set; }
    public DbSet<ServerPlan> ServerPlans { get; set; }
    public DbSet<GameServer> GameServers { get; set; }
    public DbSet<Order> Orders { get; set; }
    // ... more DbSets

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure entities, relationships, indexes
        // See database-schema.md for complete configuration
    }
}
```

## 📦 Migrations

> 📚 **See [Database Migrations Guide](../development/database-migrations.md)** for complete migration workflows and best practices.

### Install EF Core Tools

```bash
dotnet tool install --global dotnet-ef
```

### Quick Migration Commands

#### Linux/macOS (Makefile)

```bash
# Create a new migration
make db.add-migration NAME=YourMigrationName

# Apply migrations to database
make db.migrate

# Check migration status
make db.status

# Reset database (WARNING: deletes all data!)
make db.reset
```

#### Windows (PowerShell)

```powershell
# Create a new migration
.\scripts\db-migrations.ps1 -Command add-migration -Name YourMigrationName

# Apply migrations to database
.\scripts\db-migrations.ps1 -Command migrate

# Check migration status
.\scripts\db-migrations.ps1 -Command status

# Reset database (WARNING: deletes all data!)
.\scripts\db-migrations.ps1 -Command reset
```

### First-Time Setup

The UUID extension is now automatically created by migrations. Simply run:

```bash
# Linux/macOS
make db.migrate

# Windows
.\scripts\db-migrations.ps1 -Command migrate
```

## 🌱 Data Seeding

### Automatic Seeding

The `DatabaseSeeder.cs` automatically seeds the database on startup with:

**Server Types** (15 total):
- Minecraft (Basic, Standard, Premium)
- CS:GO (Basic, Standard, Premium)
- Rust (Basic, Standard, Premium)
- ARK: Survival Evolved (Basic, Standard, Premium)
- Terraria (Basic, Standard, Premium)

**Features**:
- ✅ Duplicate detection (won't reseed existing data)
- ✅ Detailed logging
- ✅ Realistic pricing and specifications
- ✅ Runs automatically on application startup in Development

### Manual Seeding

To manually trigger seeding:

```csharp
// In Program.cs or use ApplicationDbContextSeed
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}
```

### Seeded Data Details

**Minecraft Servers:**
- Basic: 2 cores, 2GB RAM, $15/mo (10 players)
- Standard: 4 cores, 4GB RAM, $30/mo (25 players)
- Premium: 8 cores, 8GB RAM, $60/mo (100 players)

**CS:GO Servers:**
- Basic: 2 cores, 2GB RAM, $22.50/mo (10 players)
- Standard: 4 cores, 4GB RAM, $45/mo (20 players)
- Premium: 8 cores, 8GB RAM, $90/mo (32 players)

See `DatabaseSeeder.cs` for complete details.

## 🏗️ Database Structure

### Key Entities

- **ApplicationUser** - ASP.NET Identity user
- **ServerType** - Game server types (Minecraft, CS:GO, etc.)
- **ServerPlan** - Pricing tiers (Basic, Standard, Premium)
- **GameServer** - Deployed server instances
- **Order** - Purchase orders
- **Payment** - Payment transactions
- **Notification** - User notifications

See [Database Schema Guide](../development/database-schema.md) for complete schema details.

## 🔍 Useful Commands

### Database Inspection

```bash
# Connect to database
docker exec -it gameserver-postgres psql -U postgres -d gameserverhosting_dev

# List tables
\dt

# Describe table
\d server_types

# Query data
SELECT * FROM server_types LIMIT 5;

# Exit
\q
```

### Database Management

```bash
# Drop database (WARNING: deletes all data)
dotnet ef database drop --project src/GameServerHosting.Api

# Recreate database
dotnet ef database update --project src/GameServerHosting.Api

# Backup database
docker exec gameserver-postgres pg_dump -U postgres gameserverhosting_dev > backup.sql

# Restore database
docker exec -i gameserver-postgres psql -U postgres gameserverhosting_dev < backup.sql
```

## 🐛 Troubleshooting

### UUID Extension Error

**Error**: `function uuid_generate_v4() does not exist`

**Solution**:
```bash
docker exec gameserver-postgres psql -U postgres -d gameserverhosting_dev -c 'CREATE EXTENSION IF NOT EXISTS "uuid-ossp";'
```

### Connection Refused

**Error**: `Failed to connect to 127.0.0.1:5432`

**Solutions**:
1. Ensure PostgreSQL container is running: `docker ps`
2. Start if not running: `docker-compose up -d postgres`
3. Check Docker Desktop is running

### Migration Already Applied

**Error**: `Migration has already been applied`

**Solution**:
```bash
# Check migration status
dotnet ef migrations list --project src/GameServerHosting.Api

# If you need to reapply, drop and recreate
dotnet ef database drop --project src/GameServerHosting.Api
dotnet ef database update --project src/GameServerHosting.Api
```

### Pending Model Changes

**Error**: `Pending model changes detected`

**Solution**:
```bash
# Create a new migration for the changes
dotnet ef migrations add <DescriptiveName> --project src/GameServerHosting.Api

# Apply it
dotnet ef database update --project src/GameServerHosting.Api
```

## 📚 Related Documentation

- [Quick Start Guide](quick-start.md) - Initial setup
- [Docker Setup](docker-setup.md) - Docker Compose details
- [Database Migrations Guide](../development/database-migrations.md) - **Complete migration workflows**
- [Database Schema](../development/database-schema.md) - Complete schema reference
- [Troubleshooting](../troubleshooting/common-issues.md) - More solutions

## 💡 Best Practices

1. **Always review migrations** before applying
2. **Use descriptive migration names**: `AddUserPreferences` not `Update1`
3. **Never modify applied migrations** - create new ones instead
4. **Test migrations locally** before committing
5. **Keep seed data idempotent** - safe to run multiple times
6. **Use transactions** for complex data operations
7. **Index frequently queried fields** for performance

---

Your database is now ready for development! 🎉


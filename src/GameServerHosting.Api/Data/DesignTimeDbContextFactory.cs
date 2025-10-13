using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameServerHosting.Api.Data;

/// <summary>
/// Design-time factory for creating DbContext instances during migrations.
/// This is used by EF Core tools (dotnet ef) to create the DbContext at design time.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GameServerDbContext>
{
    public GameServerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<GameServerDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        optionsBuilder.UseNpgsql(connectionString);

        return new GameServerDbContext(optionsBuilder.Options);
    }
}

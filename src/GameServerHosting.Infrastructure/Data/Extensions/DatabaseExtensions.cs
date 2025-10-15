using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Data.Extensions;

/// <summary>
/// Extension methods for database operations and initialization.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Ensures the database is created and all pending migrations are applied.
    /// Use this in development or for automated deployments.
    /// </summary>
    public static async Task EnsureDatabaseMigratedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameServerDbContext>();
        
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<GameServerDbContext>>();
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    /// <summary>
    /// Seeds the database with initial data if needed.
    /// </summary>
    public static Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GameServerDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GameServerDbContext>>();

        try
        {
            // Add your seed data logic here
            // Example:
            // if (!await context.GameServers.AnyAsync())
            // {
            //     context.GameServers.AddRange(
            //         new GameServer { Name = "Test Server 1" },
            //         new GameServer { Name = "Test Server 2" }
            //     );
            //     await context.SaveChangesAsync();
            // }

            logger.LogInformation("Database seeding completed successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}

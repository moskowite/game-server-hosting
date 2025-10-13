using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Api.Data;

public class GameServerDbContext : DbContext
{
    public GameServerDbContext(DbContextOptions<GameServerDbContext> options)
        : base(options)
    {
    }

    // Add your DbSet properties here as you create entities
    // Example: public DbSet<GameServer> GameServers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entity relationships and constraints here
        // Example: modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameServerDbContext).Assembly);
    }
}

using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Data;

/// <summary>
/// Seeds the database with initial data for game types, pricing tiers, and server plans
/// </summary>
public class DatabaseSeeder
{
    private readonly GameServerDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(GameServerDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all initial data
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Check if data already exists
            if (await _context.ServerTypes.AnyAsync())
            {
                _logger.LogInformation("Database already contains server types. Skipping seed.");
                return;
            }

            await SeedServerTypesAndPlansAsync();

            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    /// <summary>
    /// Seeds server types (game types with pricing tiers) and their associated billing plans
    /// </summary>
    private async Task SeedServerTypesAndPlansAsync()
    {
        var serverTypes = new List<ServerType>();

        // ========== MINECRAFT ==========
        serverTypes.AddRange(new[]
        {
            new ServerType
            {
                Name = "Minecraft Basic",
                GameName = "Minecraft",
                Description = "Perfect for small groups of friends. Runs vanilla Minecraft with basic performance.",
                CpuCores = 2,
                RamMb = 2048,
                DiskGb = 10,
                MaxPlayers = 10,
                HourlyPrice = 0.50m,
                MonthlyPrice = 15.00m,
                DockerImage = "itzg/minecraft-server:latest",
                DefaultConfig = @"{""EULA"":""TRUE"",""MODE"":""survival"",""DIFFICULTY"":""normal"",""MAX_PLAYERS"":""10""}",
                IsActive = true
            },
            new ServerType
            {
                Name = "Minecraft Standard",
                GameName = "Minecraft",
                Description = "Ideal for medium-sized communities. Supports mods and plugins with enhanced performance.",
                CpuCores = 4,
                RamMb = 4096,
                DiskGb = 25,
                MaxPlayers = 25,
                HourlyPrice = 1.00m,
                MonthlyPrice = 30.00m,
                DockerImage = "itzg/minecraft-server:latest",
                DefaultConfig = @"{""EULA"":""TRUE"",""MODE"":""survival"",""DIFFICULTY"":""normal"",""MAX_PLAYERS"":""25"",""TYPE"":""PAPER""}",
                IsActive = true
            },
            new ServerType
            {
                Name = "Minecraft Premium",
                GameName = "Minecraft",
                Description = "Enterprise-grade hosting for large communities. Maximum performance with extensive mod support.",
                CpuCores = 8,
                RamMb = 8192,
                DiskGb = 50,
                MaxPlayers = 100,
                HourlyPrice = 2.00m,
                MonthlyPrice = 60.00m,
                DockerImage = "itzg/minecraft-server:latest",
                DefaultConfig = @"{""EULA"":""TRUE"",""MODE"":""survival"",""DIFFICULTY"":""normal"",""MAX_PLAYERS"":""100"",""TYPE"":""PAPER"",""JVM_XX_OPTS"":""-XX:+UseG1GC -XX:+ParallelRefProcEnabled""}",
                IsActive = true
            }
        });

        // ========== CS:GO ==========
        serverTypes.AddRange(new[]
        {
            new ServerType
            {
                Name = "CS:GO Basic",
                GameName = "CS:GO",
                Description = "Great for casual matches and practice sessions. Standard competitive settings.",
                CpuCores = 2,
                RamMb = 2048,
                DiskGb = 30,
                MaxPlayers = 10,
                HourlyPrice = 0.75m,
                MonthlyPrice = 22.50m,
                DockerImage = "cm2network/csgo:latest",
                DefaultConfig = @"{""GSLT"":"""",""TICKRATE"":""64"",""MAXPLAYERS"":""10"",""STARTMAP"":""de_dust2""}",
                IsActive = true
            },
            new ServerType
            {
                Name = "CS:GO Standard",
                GameName = "CS:GO",
                Description = "Perfect for competitive matches. 128-tick servers with excellent performance.",
                CpuCores = 4,
                RamMb = 4096,
                DiskGb = 40,
                MaxPlayers = 20,
                HourlyPrice = 1.50m,
                MonthlyPrice = 45.00m,
                DockerImage = "cm2network/csgo:latest",
                DefaultConfig = @"{""GSLT"":"""",""TICKRATE"":""128"",""MAXPLAYERS"":""20"",""STARTMAP"":""de_dust2""}",
                IsActive = true
            },
            new ServerType
            {
                Name = "CS:GO Premium",
                GameName = "CS:GO",
                Description = "Tournament-ready servers. Ultra-low latency, 128-tick, and DDoS protection.",
                CpuCores = 8,
                RamMb = 8192,
                DiskGb = 60,
                MaxPlayers = 32,
                HourlyPrice = 3.00m,
                MonthlyPrice = 90.00m,
                DockerImage = "cm2network/csgo:latest",
                DefaultConfig = @"{""GSLT"":"""",""TICKRATE"":""128"",""MAXPLAYERS"":""32"",""STARTMAP"":""de_dust2"",""GOTV"":""true""}",
                IsActive = true
            }
        });

        // ========== RUST ==========
        serverTypes.AddRange(new[]
        {
            new ServerType
            {
                Name = "Rust Basic",
                GameName = "Rust",
                Description = "Small server for friends. Standard map size with decent performance.",
                CpuCores = 4,
                RamMb = 6144,
                DiskGb = 20,
                MaxPlayers = 50,
                HourlyPrice = 1.25m,
                MonthlyPrice = 37.50m,
                DockerImage = "didstopia/rust-server:latest",
                DefaultConfig = @"{""RUST_SERVER_WORLDSIZE"":""3000"",""RUST_SERVER_SEED"":""12345"",""RUST_SERVER_MAXPLAYERS"":""50"",""RUST_SERVER_NAME"":""My Rust Server""}",
                IsActive = true
            },
            new ServerType
            {
                Name = "Rust Standard",
                GameName = "Rust",
                Description = "Medium-sized community server. Larger maps with improved performance.",
                CpuCores = 6,
                RamMb = 10240,
                DiskGb = 30,
                MaxPlayers = 100,
                HourlyPrice = 2.50m,
                MonthlyPrice = 75.00m,
                DockerImage = "didstopia/rust-server:latest",
                DefaultConfig = @"{""RUST_SERVER_WORLDSIZE"":""4000"",""RUST_SERVER_SEED"":""12345"",""RUST_SERVER_MAXPLAYERS"":""100"",""RUST_SERVER_NAME"":""My Rust Server""}",
                IsActive = true
            },
            new ServerType
            {
                Name = "Rust Premium",
                GameName = "Rust",
                Description = "High-performance server for large communities. Maximum map size and player count.",
                CpuCores = 12,
                RamMb = 16384,
                DiskGb = 50,
                MaxPlayers = 200,
                HourlyPrice = 4.50m,
                MonthlyPrice = 135.00m,
                DockerImage = "didstopia/rust-server:latest",
                DefaultConfig = @"{""RUST_SERVER_WORLDSIZE"":""5000"",""RUST_SERVER_SEED"":""12345"",""RUST_SERVER_MAXPLAYERS"":""200"",""RUST_SERVER_NAME"":""My Rust Server"",""RUST_RCON_PORT"":""28016""}",
                IsActive = true
            }
        });

        // ========== ARK: SURVIVAL EVOLVED ==========
        serverTypes.AddRange(new[]
        {
            new ServerType
            {
                Name = "ARK Basic",
                GameName = "ARK",
                Description = "Entry-level ARK server. Perfect for small tribes exploring The Island.",
                CpuCores = 4,
                RamMb = 6144,
                DiskGb = 30,
                MaxPlayers = 20,
                HourlyPrice = 1.50m,
                MonthlyPrice = 45.00m,
                DockerImage = "steamcmd/steamcmd:latest",
                DefaultConfig = @"{""SessionName"":""My ARK Server"",""ServerPassword"":"""",""MaxPlayers"":20,""ServerMap"":""TheIsland""}",
                IsActive = true
            },
            new ServerType
            {
                Name = "ARK Standard",
                GameName = "ARK",
                Description = "Mid-tier ARK hosting. Supports medium tribes with enhanced taming rates.",
                CpuCores = 6,
                RamMb = 10240,
                DiskGb = 50,
                MaxPlayers = 50,
                HourlyPrice = 2.75m,
                MonthlyPrice = 82.50m,
                DockerImage = "steamcmd/steamcmd:latest",
                DefaultConfig = @"{""SessionName"":""My ARK Server"",""ServerPassword"":"""",""MaxPlayers"":50,""ServerMap"":""TheIsland"",""TamingSpeedMultiplier"":3}",
                IsActive = true
            },
            new ServerType
            {
                Name = "ARK Premium",
                GameName = "ARK",
                Description = "Premium ARK experience. Large-scale battles, all maps, and custom configurations.",
                CpuCores = 12,
                RamMb = 16384,
                DiskGb = 100,
                MaxPlayers = 100,
                HourlyPrice = 5.00m,
                MonthlyPrice = 150.00m,
                DockerImage = "steamcmd/steamcmd:latest",
                DefaultConfig = @"{""SessionName"":""My ARK Server"",""ServerPassword"":"""",""MaxPlayers"":100,""ServerMap"":""TheIsland"",""TamingSpeedMultiplier"":3,""HarvestAmountMultiplier"":2}",
                IsActive = true
            }
        });

        // ========== VALHEIM ==========
        serverTypes.AddRange(new[]
        {
            new ServerType
            {
                Name = "Valheim Basic",
                GameName = "Valheim",
                Description = "Cozy Valheim server for small groups of Vikings. Explore and build together.",
                CpuCores = 2,
                RamMb = 4096,
                DiskGb = 15,
                MaxPlayers = 5,
                HourlyPrice = 0.60m,
                MonthlyPrice = 18.00m,
                DockerImage = "lloesche/valheim-server:latest",
                DefaultConfig = @"{""SERVER_NAME"":""My Valheim Server"",""WORLD_NAME"":""Dedicated"",""SERVER_PASS"":"""",""SERVER_PUBLIC"":""true""}",
                IsActive = true
            },
            new ServerType
            {
                Name = "Valheim Standard",
                GameName = "Valheim",
                Description = "Great for medium clans. Smooth performance for extended adventures.",
                CpuCores = 4,
                RamMb = 6144,
                DiskGb = 25,
                MaxPlayers = 10,
                HourlyPrice = 1.10m,
                MonthlyPrice = 33.00m,
                DockerImage = "lloesche/valheim-server:latest",
                DefaultConfig = @"{""SERVER_NAME"":""My Valheim Server"",""WORLD_NAME"":""Dedicated"",""SERVER_PASS"":"""",""SERVER_PUBLIC"":""true""}",
                IsActive = true
            },
            new ServerType
            {
                Name = "Valheim Premium",
                GameName = "Valheim",
                Description = "Premium Valheim hosting. Large communities, modding support, and optimal performance.",
                CpuCores = 6,
                RamMb = 8192,
                DiskGb = 40,
                MaxPlayers = 20,
                HourlyPrice = 1.80m,
                MonthlyPrice = 54.00m,
                DockerImage = "lloesche/valheim-server:latest",
                DefaultConfig = @"{""SERVER_NAME"":""My Valheim Server"",""WORLD_NAME"":""Dedicated"",""SERVER_PASS"":"""",""SERVER_PUBLIC"":""true"",""VALHEIM_PLUS"":""true""}",
                IsActive = true
            }
        });

        // Add all server types to context
        await _context.ServerTypes.AddRangeAsync(serverTypes);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} server types across 5 games (Minecraft, CS:GO, Rust, ARK, Valheim)", serverTypes.Count);

        // Create billing plans for each server type
        var serverPlans = new List<ServerPlan>();

        foreach (var serverType in serverTypes)
        {
            // Hourly Plan
            serverPlans.Add(new ServerPlan
            {
                ServerTypeId = serverType.Id,
                Name = "Hourly",
                BillingCycle = BillingCycle.Hourly,
                Price = serverType.HourlyPrice,
                Currency = "USD",
                DurationHours = 1,
                DiscountPercentage = 0,
                IsActive = true,
                DisplayOrder = 1,
                Description = "Pay-as-you-go hourly billing. Perfect for testing or short gaming sessions."
            });

            // Daily Plan (5% discount)
            var dailyPrice = serverType.HourlyPrice * 24 * 0.95m;
            serverPlans.Add(new ServerPlan
            {
                ServerTypeId = serverType.Id,
                Name = "Daily",
                BillingCycle = BillingCycle.Daily,
                Price = dailyPrice,
                Currency = "USD",
                DurationHours = 24,
                DiscountPercentage = 5,
                IsActive = true,
                DisplayOrder = 2,
                Description = "24-hour access with 5% discount. Great for weekend gaming sessions."
            });

            // Weekly Plan (10% discount)
            var weeklyPrice = serverType.HourlyPrice * 24 * 7 * 0.90m;
            serverPlans.Add(new ServerPlan
            {
                ServerTypeId = serverType.Id,
                Name = "Weekly",
                BillingCycle = BillingCycle.Weekly,
                Price = weeklyPrice,
                Currency = "USD",
                DurationHours = 168,
                DiscountPercentage = 10,
                IsActive = true,
                DisplayOrder = 3,
                Description = "One week of hosting with 10% savings. Ideal for short-term events."
            });

            // Monthly Plan (matches the pre-set monthly price, typically 15-20% discount)
            var monthlyDiscountPercentage = Math.Round((1 - (serverType.MonthlyPrice / (serverType.HourlyPrice * 720))) * 100, 2);
            serverPlans.Add(new ServerPlan
            {
                ServerTypeId = serverType.Id,
                Name = "Monthly",
                BillingCycle = BillingCycle.Monthly,
                Price = serverType.MonthlyPrice,
                Currency = "USD",
                DurationHours = 720, // 30 days
                DiscountPercentage = monthlyDiscountPercentage,
                IsActive = true,
                DisplayOrder = 4,
                Description = $"30 days of hosting with {monthlyDiscountPercentage}% discount. Most popular choice!"
            });

            // Quarterly Plan (25% discount)
            var quarterlyPrice = serverType.MonthlyPrice * 3 * 0.75m;
            var quarterlyDiscountPercentage = Math.Round((1 - (quarterlyPrice / (serverType.HourlyPrice * 2160))) * 100, 2);
            serverPlans.Add(new ServerPlan
            {
                ServerTypeId = serverType.Id,
                Name = "Quarterly",
                BillingCycle = BillingCycle.Quarterly,
                Price = quarterlyPrice,
                Currency = "USD",
                DurationHours = 2160, // 90 days
                DiscountPercentage = quarterlyDiscountPercentage,
                IsActive = true,
                DisplayOrder = 5,
                Description = $"3 months of hosting with {quarterlyDiscountPercentage}% savings. Great value for dedicated communities."
            });

            // Annual Plan (30% discount)
            var annualPrice = serverType.MonthlyPrice * 12 * 0.70m;
            var annualDiscountPercentage = Math.Round((1 - (annualPrice / (serverType.HourlyPrice * 8760))) * 100, 2);
            serverPlans.Add(new ServerPlan
            {
                ServerTypeId = serverType.Id,
                Name = "Annual",
                BillingCycle = BillingCycle.Annual,
                Price = annualPrice,
                Currency = "USD",
                DurationHours = 8760, // 365 days
                DiscountPercentage = annualDiscountPercentage,
                IsActive = true,
                DisplayOrder = 6,
                Description = $"Full year of hosting with {annualDiscountPercentage}% discount. Best value for serious communities!"
            });
        }

        await _context.ServerPlans.AddRangeAsync(serverPlans);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} server plans (6 billing cycles per server type)", serverPlans.Count);
    }
}

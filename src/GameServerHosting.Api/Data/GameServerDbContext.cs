using GameServerHosting.Shared.Entities;
using GameServerHosting.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Api.Data;

public class GameServerDbContext : DbContext
{
    public GameServerDbContext(DbContextOptions<GameServerDbContext> options)
        : base(options)
    {
    }

    // DbSet properties
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ServerType> ServerTypes { get; set; } = null!;
    public DbSet<GameServer> GameServers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== User Configuration =====
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.Username)
                .HasColumnName("username")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(20);
            
            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at");
            
            // Indexes
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Role);
            
            // Relationships
            entity.HasMany(e => e.GameServers)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Orders)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== ServerType Configuration =====
        modelBuilder.Entity<ServerType>(entity =>
        {
            entity.ToTable("server_types");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.GameName)
                .HasColumnName("game_name")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .IsRequired();
            
            entity.Property(e => e.CpuCores)
                .HasColumnName("cpu_cores")
                .IsRequired();
            
            entity.Property(e => e.RamMb)
                .HasColumnName("ram_mb")
                .IsRequired();
            
            entity.Property(e => e.DiskGb)
                .HasColumnName("disk_gb")
                .IsRequired();
            
            entity.Property(e => e.MaxPlayers)
                .HasColumnName("max_players")
                .IsRequired();
            
            entity.Property(e => e.HourlyPrice)
                .HasColumnName("hourly_price")
                .HasColumnType("decimal(10,2)")
                .IsRequired();
            
            entity.Property(e => e.MonthlyPrice)
                .HasColumnName("monthly_price")
                .HasColumnType("decimal(10,2)")
                .IsRequired();
            
            entity.Property(e => e.DockerImage)
                .HasColumnName("docker_image")
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.DefaultConfig)
                .HasColumnName("default_config")
                .HasColumnType("jsonb")
                .HasDefaultValue("{}")
                .IsRequired();
            
            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.GameName);
            entity.HasIndex(e => e.IsActive);
            
            // Relationships
            entity.HasMany(e => e.GameServers)
                .WithOne(e => e.ServerType)
                .HasForeignKey(e => e.ServerTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== GameServer Configuration =====
        modelBuilder.Entity<GameServer>(entity =>
        {
            entity.ToTable("game_servers");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.ServerTypeId)
                .HasColumnName("server_type_id")
                .IsRequired();
            
            entity.Property(e => e.ServerName)
                .HasColumnName("server_name")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(45);
            
            entity.Property(e => e.Port)
                .HasColumnName("port");
            
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.Configuration)
                .HasColumnName("configuration")
                .HasColumnType("jsonb")
                .HasDefaultValue("{}")
                .IsRequired();
            
            entity.Property(e => e.Version)
                .HasColumnName("version")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.DeployedAt)
                .HasColumnName("deployed_at");
            
            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");
            
            entity.Property(e => e.LastHeartbeatAt)
                .HasColumnName("last_heartbeat_at");
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ServerTypeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.ServerName }).IsUnique();
            
            // Relationships configured in User and ServerType entities
            entity.HasMany(e => e.Orders)
                .WithOne(e => e.GameServer)
                .HasForeignKey(e => e.ServerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== Order Configuration =====
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.ServerId)
                .HasColumnName("server_id");
            
            entity.Property(e => e.OrderNumber)
                .HasColumnName("order_number")
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.OrderType)
                .HasColumnName("order_type")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.TotalAmount)
                .HasColumnName("total_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();
            
            entity.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .HasDefaultValue("USD")
                .IsRequired();
            
            entity.Property(e => e.PaymentMethod)
                .HasColumnName("payment_method")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.PaymentTransactionId)
                .HasColumnName("payment_transaction_id")
                .HasMaxLength(255);
            
            entity.Property(e => e.PaymentDate)
                .HasColumnName("payment_date");
            
            entity.Property(e => e.BillingDetails)
                .HasColumnName("billing_details")
                .HasColumnType("jsonb")
                .HasDefaultValue("{}")
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ServerId);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            
            // Relationships configured in User and GameServer entities
        });
    }
}

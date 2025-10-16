using GameServerHosting.Domain.Entities;
using GameServerHosting.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GameServerHosting.Infrastructure.Data;

public class GameServerDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public GameServerDbContext(DbContextOptions<GameServerDbContext> options)
        : base(options)
    {
    }

    // DbSet properties
    public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;
    public DbSet<ServerType> ServerTypes { get; set; } = null!;
    public DbSet<ServerPlan> ServerPlans { get; set; } = null!;
    public DbSet<GameServer> GameServers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<UserPreferences> UserPreferences { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<AsyncOperation> AsyncOperations { get; set; } = null!;
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Identity table names to match PostgreSQL naming convention
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            
            // Custom properties
            entity.Property(e => e.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();
            
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
            
            // Override Identity default column names
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserName).HasColumnName("username");
            entity.Property(e => e.NormalizedUserName).HasColumnName("normalized_username");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email");
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            entity.Property(e => e.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(e => e.LockoutEnabled).HasColumnName("lockout_enabled");
            entity.Property(e => e.AccessFailedCount).HasColumnName("access_failed_count");
            
            // Custom indexes
            entity.HasIndex(e => e.Role).HasDatabaseName("idx_users_role");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_users_is_active");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_users_created_at");
            
            // Relationships
            entity.HasMany(e => e.GameServers)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Orders)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasMany(e => e.Payments)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== UserPreferences Configuration =====
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.ToTable("user_preferences");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.EmailPromotional)
                .HasColumnName("email_promotional")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.EmailOrderUpdates)
                .HasColumnName("email_order_updates")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.EmailServerAlerts)
                .HasColumnName("email_server_alerts")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.EmailNewsletter)
                .HasColumnName("email_newsletter")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.NotificationsBrowser)
                .HasColumnName("notifications_browser")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.NotificationsServerOffline)
                .HasColumnName("notifications_server_offline")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.NotificationsResourceAlerts)
                .HasColumnName("notifications_resource_alerts")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.NotificationsPaymentDue)
                .HasColumnName("notifications_payment_due")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.NotificationsSecurityAlerts)
                .HasColumnName("notifications_security_alerts")
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
            entity.HasIndex(e => e.UserId)
                .IsUnique()
                .HasDatabaseName("idx_user_preferences_user_id");
            
            // Relationships
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure other Identity tables
        modelBuilder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("roles");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.NormalizedName).HasColumnName("normalized_name");
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp");
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
            entity.Property(e => e.LoginProvider).HasColumnName("login_provider");
            entity.Property(e => e.ProviderKey).HasColumnName("provider_key");
            entity.Property(e => e.ProviderDisplayName).HasColumnName("provider_display_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.LoginProvider).HasColumnName("login_provider");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue).HasColumnName("claim_value");
        });

        // ===== ServerType Configuration =====
        modelBuilder.Entity<ServerType>(entity =>
        {
            entity.ToTable("server_types", t =>
            {
                t.HasCheckConstraint("ck_server_types_cpu_cores", "cpu_cores > 0");
                t.HasCheckConstraint("ck_server_types_ram_mb", "ram_mb > 0");
                t.HasCheckConstraint("ck_server_types_disk_gb", "disk_gb > 0");
                t.HasCheckConstraint("ck_server_types_max_players", "max_players > 0");
                t.HasCheckConstraint("ck_server_types_hourly_price", "hourly_price >= 0");
                t.HasCheckConstraint("ck_server_types_monthly_price", "monthly_price >= 0");
            });
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
            
            entity.Property(e => e.DefaultVersion)
                .HasColumnName("default_version")
                .HasMaxLength(50);
            
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
            entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("idx_server_types_name");
            entity.HasIndex(e => e.GameName).HasDatabaseName("idx_server_types_game_name");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_server_types_is_active");
            entity.HasIndex(e => new { e.GameName, e.IsActive }).HasDatabaseName("idx_server_types_game_active");
            
            // Relationships
            entity.HasMany(e => e.GameServers)
                .WithOne(e => e.ServerType)
                .HasForeignKey(e => e.ServerTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasMany(e => e.ServerPlans)
                .WithOne(e => e.ServerType)
                .HasForeignKey(e => e.ServerTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== GameServer Configuration =====
        modelBuilder.Entity<GameServer>(entity =>
        {
            entity.ToTable("game_servers", t =>
            {
                t.HasCheckConstraint("ck_game_servers_port", "port IS NULL OR (port >= 1 AND port <= 65535)");
            });
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
            
            // Provisioning fields
            entity.Property(e => e.ProviderType)
                .HasColumnName("provider_type")
                .HasMaxLength(50);
            
            entity.Property(e => e.ProviderId)
                .HasColumnName("provider_id")
                .HasMaxLength(100);
            
            entity.Property(e => e.ProviderRegion)
                .HasColumnName("provider_region")
                .HasMaxLength(50);
            
            entity.Property(e => e.ProvisioningStartedAt)
                .HasColumnName("provisioning_started_at");
            
            entity.Property(e => e.ProvisioningCompletedAt)
                .HasColumnName("provisioning_completed_at");
            
            entity.Property(e => e.ProvisioningError)
                .HasColumnName("provisioning_error");
            
            entity.Property(e => e.SshKeyPrivate)
                .HasColumnName("ssh_key_private");
            
            entity.Property(e => e.SshKeyPublic)
                .HasColumnName("ssh_key_public");
            
            entity.Property(e => e.HealthCheckStatus)
                .HasColumnName("health_check_status")
                .HasMaxLength(50);
            
            // Indexes
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_game_servers_user_id");
            entity.HasIndex(e => e.ServerTypeId).HasDatabaseName("idx_game_servers_server_type_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_game_servers_status");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_game_servers_expires_at");
            entity.HasIndex(e => new { e.UserId, e.ServerName }).IsUnique().HasDatabaseName("idx_game_servers_user_server_name");
            entity.HasIndex(e => new { e.IpAddress, e.Port })
                .IsUnique()
                .HasDatabaseName("idx_game_servers_ip_port")
                .HasFilter("ip_address IS NOT NULL AND port IS NOT NULL");
            entity.HasIndex(e => new { e.Status, e.ExpiresAt }).HasDatabaseName("idx_game_servers_status_expires");
            entity.HasIndex(e => e.LastHeartbeatAt).HasDatabaseName("idx_game_servers_last_heartbeat");
            
            // Relationships configured in User and ServerType entities
            entity.HasMany(e => e.Orders)
                .WithOne(e => e.GameServer)
                .HasForeignKey(e => e.ServerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== Order Configuration =====
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders", t =>
            {
                t.HasCheckConstraint("ck_orders_total_amount", "total_amount >= 0");
            });
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.ServerId)
                .HasColumnName("server_id");
            
            entity.Property(e => e.ServerPlanId)
                .HasColumnName("server_plan_id");
            
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
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_orders_user_id");
            entity.HasIndex(e => e.ServerId).HasDatabaseName("idx_orders_server_id");
            entity.HasIndex(e => e.ServerPlanId).HasDatabaseName("idx_orders_server_plan_id");
            entity.HasIndex(e => e.OrderNumber).IsUnique().HasDatabaseName("idx_orders_order_number");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_orders_status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_orders_created_at");
            entity.HasIndex(e => new { e.UserId, e.Status }).HasDatabaseName("idx_orders_user_status");
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }).HasDatabaseName("idx_orders_user_created");
            entity.HasIndex(e => e.PaymentDate).HasDatabaseName("idx_orders_payment_date");
            
            // Relationships configured in User and GameServer entities
            entity.HasOne(e => e.ServerPlan)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.ServerPlanId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasMany(e => e.Payments)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== ServerPlan Configuration =====
        modelBuilder.Entity<ServerPlan>(entity =>
        {
            entity.ToTable("server_plans", t =>
            {
                t.HasCheckConstraint("ck_server_plans_price", "price >= 0");
                t.HasCheckConstraint("ck_server_plans_duration", "duration_hours > 0");
                t.HasCheckConstraint("ck_server_plans_discount", "discount_percentage >= 0 AND discount_percentage <= 100");
            });
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.ServerTypeId)
                .HasColumnName("server_type_id")
                .IsRequired();
            
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.BillingCycle)
                .HasColumnName("billing_cycle")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.Price)
                .HasColumnName("price")
                .HasColumnType("decimal(10,2)")
                .IsRequired();
            
            entity.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .HasDefaultValue("USD")
                .IsRequired();
            
            entity.Property(e => e.DurationHours)
                .HasColumnName("duration_hours")
                .IsRequired();
            
            entity.Property(e => e.DiscountPercentage)
                .HasColumnName("discount_percentage")
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(0)
                .IsRequired();
            
            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();
            
            entity.Property(e => e.DisplayOrder)
                .HasColumnName("display_order")
                .HasDefaultValue(0)
                .IsRequired();
            
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasDefaultValue(string.Empty)
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
            entity.HasIndex(e => e.ServerTypeId).HasDatabaseName("idx_server_plans_server_type_id");
            entity.HasIndex(e => new { e.ServerTypeId, e.BillingCycle }).IsUnique().HasDatabaseName("idx_server_plans_type_cycle");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_server_plans_is_active");
            entity.HasIndex(e => e.DisplayOrder).HasDatabaseName("idx_server_plans_display_order");
            entity.HasIndex(e => new { e.ServerTypeId, e.IsActive, e.DisplayOrder }).HasDatabaseName("idx_server_plans_type_active_order");
            
            // Relationships configured in ServerType entity
        });

        // ===== Payment Configuration =====
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments", t =>
            {
                t.HasCheckConstraint("ck_payments_amount", "amount >= 0");
                t.HasCheckConstraint("ck_payments_refund_amount", "refund_amount IS NULL OR (refund_amount >= 0 AND refund_amount <= amount)");
            });
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.OrderId)
                .HasColumnName("order_id")
                .IsRequired();
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.Amount)
                .HasColumnName("amount")
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
            
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.TransactionId)
                .HasColumnName("transaction_id")
                .HasMaxLength(255);
            
            entity.Property(e => e.PaymentGateway)
                .HasColumnName("payment_gateway")
                .HasMaxLength(50);
            
            entity.Property(e => e.PaymentIdentifier)
                .HasColumnName("payment_identifier")
                .HasMaxLength(100);
            
            entity.Property(e => e.GatewayResponse)
                .HasColumnName("gateway_response")
                .HasColumnType("jsonb")
                .HasDefaultValue("{}")
                .IsRequired();
            
            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message");
            
            entity.Property(e => e.ErrorCode)
                .HasColumnName("error_code")
                .HasMaxLength(50);
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.CompletedAt)
                .HasColumnName("completed_at");
            
            entity.Property(e => e.RefundedAt)
                .HasColumnName("refunded_at");
            
            entity.Property(e => e.RefundAmount)
                .HasColumnName("refund_amount")
                .HasColumnType("decimal(10,2)");
            
            entity.Property(e => e.RefundReason)
                .HasColumnName("refund_reason");
            
            entity.Property(e => e.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(45);
            
            entity.Property(e => e.UserAgent)
                .HasColumnName("user_agent")
                .HasMaxLength(500);
            
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .HasDefaultValue("{}")
                .IsRequired();
            
            // Indexes
            entity.HasIndex(e => e.OrderId).HasDatabaseName("idx_payments_order_id");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_payments_user_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_payments_status");
            entity.HasIndex(e => e.TransactionId).HasDatabaseName("idx_payments_transaction_id");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_payments_created_at");
            entity.HasIndex(e => new { e.UserId, e.Status }).HasDatabaseName("idx_payments_user_status");
            entity.HasIndex(e => new { e.OrderId, e.Status }).HasDatabaseName("idx_payments_order_status");
            entity.HasIndex(e => e.CompletedAt).HasDatabaseName("idx_payments_completed_at");
            entity.HasIndex(e => e.RefundedAt).HasDatabaseName("idx_payments_refunded_at");
            
            // Relationships configured in Order and User entities
        });

        // ===== CartItem Configuration =====
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("cart_items", t =>
            {
                t.HasCheckConstraint("ck_cart_items_quantity", "quantity > 0");
                t.HasCheckConstraint("ck_cart_items_price", "price >= 0");
            });
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.ServerPlanId)
                .HasColumnName("server_plan_id")
                .IsRequired();
            
            entity.Property(e => e.Quantity)
                .HasColumnName("quantity")
                .HasDefaultValue(1)
                .IsRequired();
            
            entity.Property(e => e.Price)
                .HasColumnName("price")
                .HasColumnType("decimal(10,2)")
                .IsRequired();
            
            entity.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .HasDefaultValue("USD")
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
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_cart_items_user_id");
            entity.HasIndex(e => e.ServerPlanId).HasDatabaseName("idx_cart_items_server_plan_id");
            entity.HasIndex(e => new { e.UserId, e.ServerPlanId })
                .IsUnique()
                .HasDatabaseName("idx_cart_items_user_plan");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_cart_items_created_at");
            
            // Relationships
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.ServerPlan)
                .WithMany()
                .HasForeignKey(e => e.ServerPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== OutboxMessage Configuration =====
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(500)
                .IsRequired();
            
            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.ProcessedAt)
                .HasColumnName("processed_at");
            
            entity.Property(e => e.Error)
                .HasColumnName("error");
            
            entity.Property(e => e.ProcessingAttempts)
                .HasColumnName("processing_attempts")
                .HasDefaultValue(0)
                .IsRequired();
            
            entity.Property(e => e.CorrelationId)
                .HasColumnName("correlation_id");
            
            // Indexes
            entity.HasIndex(e => e.ProcessedAt).HasDatabaseName("idx_outbox_messages_processed_at");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_outbox_messages_created_at");
            entity.HasIndex(e => e.CorrelationId).HasDatabaseName("idx_outbox_messages_correlation_id");
            entity.HasIndex(e => new { e.ProcessedAt, e.CreatedAt })
                .HasDatabaseName("idx_outbox_messages_pending")
                .HasFilter("processed_at IS NULL");
        });

        // ===== AsyncOperation Configuration =====
        modelBuilder.Entity<AsyncOperation>(entity =>
        {
            entity.ToTable("async_operations");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.OperationType)
                .HasColumnName("operation_type")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            
            entity.Property(e => e.ResourceId)
                .HasColumnName("resource_id");
            
            entity.Property(e => e.ResourceType)
                .HasColumnName("resource_type")
                .HasMaxLength(100);
            
            entity.Property(e => e.CorrelationId)
                .HasColumnName("correlation_id")
                .IsRequired();
            
            entity.Property(e => e.ProgressPercentage)
                .HasColumnName("progress_percentage");
            
            entity.Property(e => e.StatusMessage)
                .HasColumnName("status_message")
                .HasMaxLength(500);
            
            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message");
            
            entity.Property(e => e.ErrorCode)
                .HasColumnName("error_code")
                .HasMaxLength(50);
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.CompletedAt)
                .HasColumnName("completed_at");
            
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");
            
            // Indexes
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_async_operations_user_id");
            entity.HasIndex(e => e.CorrelationId).IsUnique().HasDatabaseName("idx_async_operations_correlation_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_async_operations_status");
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId }).HasDatabaseName("idx_async_operations_resource");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_async_operations_created_at");
            entity.HasIndex(e => new { e.UserId, e.Status }).HasDatabaseName("idx_async_operations_user_status");
            
            // Relationships
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== IdempotencyKey Configuration =====
        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("idempotency_keys");
            entity.HasKey(e => e.Key);
            
            entity.Property(e => e.Key)
                .HasColumnName("key")
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.RequestType)
                .HasColumnName("request_type")
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.ResourceId)
                .HasColumnName("resource_id");
            
            entity.Property(e => e.StatusCode)
                .HasColumnName("status_code")
                .IsRequired();
            
            entity.Property(e => e.ResponseBody)
                .HasColumnName("response_body")
                .HasColumnType("jsonb");
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
            
            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");
            
            // Indexes
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_idempotency_keys_user_id");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_idempotency_keys_expires_at");
            entity.HasIndex(e => new { e.UserId, e.RequestType }).HasDatabaseName("idx_idempotency_keys_user_request");
        });
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameServerHosting.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedEntityRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "server_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    game_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    cpu_cores = table.Column<int>(type: "integer", nullable: false),
                    ram_mb = table.Column<int>(type: "integer", nullable: false),
                    disk_gb = table.Column<int>(type: "integer", nullable: false),
                    max_players = table.Column<int>(type: "integer", nullable: false),
                    hourly_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    monthly_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    docker_image = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    default_config = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_types", x => x.id);
                    table.CheckConstraint("ck_server_types_cpu_cores", "cpu_cores > 0");
                    table.CheckConstraint("ck_server_types_disk_gb", "disk_gb > 0");
                    table.CheckConstraint("ck_server_types_hourly_price", "hourly_price >= 0");
                    table.CheckConstraint("ck_server_types_max_players", "max_players > 0");
                    table.CheckConstraint("ck_server_types_monthly_price", "monthly_price >= 0");
                    table.CheckConstraint("ck_server_types_ram_mb", "ram_mb > 0");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "server_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    server_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    billing_cycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    duration_hours = table.Column<int>(type: "integer", nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    description = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server_plans", x => x.id);
                    table.CheckConstraint("ck_server_plans_discount", "discount_percentage >= 0 AND discount_percentage <= 100");
                    table.CheckConstraint("ck_server_plans_duration", "duration_hours > 0");
                    table.CheckConstraint("ck_server_plans_price", "price >= 0");
                    table.ForeignKey(
                        name: "FK_server_plans_server_types_server_type_id",
                        column: x => x.server_type_id,
                        principalTable: "server_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_servers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    server_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    server_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    port = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    configuration = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    deployed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_servers", x => x.id);
                    table.CheckConstraint("ck_game_servers_port", "port IS NULL OR (port >= 1 AND port <= 65535)");
                    table.ForeignKey(
                        name: "FK_game_servers_server_types_server_type_id",
                        column: x => x.server_type_id,
                        principalTable: "server_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_servers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    server_id = table.Column<Guid>(type: "uuid", nullable: true),
                    server_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_transaction_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    billing_details = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("ck_orders_total_amount", "total_amount >= 0");
                    table.ForeignKey(
                        name: "FK_orders_game_servers_server_id",
                        column: x => x.server_id,
                        principalTable: "game_servers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_orders_server_plans_server_plan_id",
                        column: x => x.server_plan_id,
                        principalTable: "server_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    payment_gateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gateway_response = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    error_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refund_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    refund_reason = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount", "amount >= 0");
                    table.CheckConstraint("ck_payments_refund_amount", "refund_amount IS NULL OR (refund_amount >= 0 AND refund_amount <= amount)");
                    table.ForeignKey(
                        name: "FK_payments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_game_servers_expires_at",
                table: "game_servers",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_game_servers_ip_port",
                table: "game_servers",
                columns: new[] { "ip_address", "port" },
                unique: true,
                filter: "ip_address IS NOT NULL AND port IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_game_servers_last_heartbeat",
                table: "game_servers",
                column: "last_heartbeat_at");

            migrationBuilder.CreateIndex(
                name: "idx_game_servers_server_type_id",
                table: "game_servers",
                column: "server_type_id");

            migrationBuilder.CreateIndex(
                name: "idx_game_servers_status",
                table: "game_servers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_game_servers_status_expires",
                table: "game_servers",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "idx_game_servers_user_id",
                table: "game_servers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_game_servers_user_server_name",
                table: "game_servers",
                columns: new[] { "user_id", "server_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_orders_created_at",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_orders_payment_date",
                table: "orders",
                column: "payment_date");

            migrationBuilder.CreateIndex(
                name: "idx_orders_server_id",
                table: "orders",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_server_plan_id",
                table: "orders",
                column: "server_plan_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_orders_user_created",
                table: "orders",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_orders_user_id",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_user_status",
                table: "orders",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_payments_completed_at",
                table: "payments",
                column: "completed_at");

            migrationBuilder.CreateIndex(
                name: "idx_payments_created_at",
                table: "payments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_payments_order_id",
                table: "payments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_payments_order_status",
                table: "payments",
                columns: new[] { "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_payments_refunded_at",
                table: "payments",
                column: "refunded_at");

            migrationBuilder.CreateIndex(
                name: "idx_payments_status",
                table: "payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_payments_transaction_id",
                table: "payments",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "idx_payments_user_id",
                table: "payments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_payments_user_status",
                table: "payments",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_server_plans_display_order",
                table: "server_plans",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "idx_server_plans_is_active",
                table: "server_plans",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_server_plans_server_type_id",
                table: "server_plans",
                column: "server_type_id");

            migrationBuilder.CreateIndex(
                name: "idx_server_plans_type_active_order",
                table: "server_plans",
                columns: new[] { "server_type_id", "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "idx_server_plans_type_cycle",
                table: "server_plans",
                columns: new[] { "server_type_id", "billing_cycle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_server_types_game_active",
                table: "server_types",
                columns: new[] { "game_name", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_server_types_game_name",
                table: "server_types",
                column: "game_name");

            migrationBuilder.CreateIndex(
                name: "idx_server_types_is_active",
                table: "server_types",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_server_types_name",
                table: "server_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "idx_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "game_servers");

            migrationBuilder.DropTable(
                name: "server_plans");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "server_types");
        }
    }
}

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE server_types (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    name character varying(100) NOT NULL,
    game_name character varying(100) NOT NULL,
    description text NOT NULL,
    cpu_cores integer NOT NULL,
    ram_mb integer NOT NULL,
    disk_gb integer NOT NULL,
    max_players integer NOT NULL,
    hourly_price numeric(10,2) NOT NULL,
    monthly_price numeric(10,2) NOT NULL,
    docker_image character varying(255) NOT NULL,
    default_config jsonb NOT NULL DEFAULT '{}',
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_server_types" PRIMARY KEY (id),
    CONSTRAINT ck_server_types_cpu_cores CHECK (cpu_cores > 0),
    CONSTRAINT ck_server_types_disk_gb CHECK (disk_gb > 0),
    CONSTRAINT ck_server_types_hourly_price CHECK (hourly_price >= 0),
    CONSTRAINT ck_server_types_max_players CHECK (max_players > 0),
    CONSTRAINT ck_server_types_monthly_price CHECK (monthly_price >= 0),
    CONSTRAINT ck_server_types_ram_mb CHECK (ram_mb > 0)
);

CREATE TABLE users (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    email character varying(255) NOT NULL,
    username character varying(50) NOT NULL,
    password_hash character varying(255) NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    phone_number character varying(20),
    role character varying(20) NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    last_login_at timestamp with time zone,
    CONSTRAINT "PK_users" PRIMARY KEY (id)
);

CREATE TABLE server_plans (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    server_type_id uuid NOT NULL,
    name character varying(100) NOT NULL,
    billing_cycle character varying(20) NOT NULL,
    price numeric(10,2) NOT NULL,
    currency character varying(3) NOT NULL DEFAULT 'USD',
    duration_hours integer NOT NULL,
    discount_percentage numeric(5,2) NOT NULL DEFAULT 0.0,
    is_active boolean NOT NULL DEFAULT TRUE,
    display_order integer NOT NULL DEFAULT 0,
    description text NOT NULL DEFAULT '',
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_server_plans" PRIMARY KEY (id),
    CONSTRAINT ck_server_plans_discount CHECK (discount_percentage >= 0 AND discount_percentage <= 100),
    CONSTRAINT ck_server_plans_duration CHECK (duration_hours > 0),
    CONSTRAINT ck_server_plans_price CHECK (price >= 0),
    CONSTRAINT "FK_server_plans_server_types_server_type_id" FOREIGN KEY (server_type_id) REFERENCES server_types (id) ON DELETE RESTRICT
);

CREATE TABLE game_servers (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    user_id uuid NOT NULL,
    server_type_id uuid NOT NULL,
    server_name character varying(100) NOT NULL,
    ip_address character varying(45),
    port integer,
    status character varying(20) NOT NULL,
    configuration jsonb NOT NULL DEFAULT '{}',
    version character varying(50) NOT NULL,
    deployed_at timestamp with time zone,
    expires_at timestamp with time zone,
    last_heartbeat_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_game_servers" PRIMARY KEY (id),
    CONSTRAINT ck_game_servers_port CHECK (port IS NULL OR (port >= 1 AND port <= 65535)),
    CONSTRAINT "FK_game_servers_server_types_server_type_id" FOREIGN KEY (server_type_id) REFERENCES server_types (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_game_servers_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

CREATE TABLE orders (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    user_id uuid NOT NULL,
    server_id uuid,
    server_plan_id uuid,
    order_number character varying(50) NOT NULL,
    order_type character varying(20) NOT NULL,
    status character varying(20) NOT NULL,
    total_amount numeric(10,2) NOT NULL,
    currency character varying(3) NOT NULL DEFAULT 'USD',
    payment_method character varying(20) NOT NULL,
    payment_transaction_id character varying(255),
    payment_date timestamp with time zone,
    billing_details jsonb NOT NULL DEFAULT '{}',
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_orders" PRIMARY KEY (id),
    CONSTRAINT ck_orders_total_amount CHECK (total_amount >= 0),
    CONSTRAINT "FK_orders_game_servers_server_id" FOREIGN KEY (server_id) REFERENCES game_servers (id) ON DELETE SET NULL,
    CONSTRAINT "FK_orders_server_plans_server_plan_id" FOREIGN KEY (server_plan_id) REFERENCES server_plans (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_orders_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);

CREATE TABLE payments (
    id uuid NOT NULL DEFAULT (uuid_generate_v4()),
    order_id uuid NOT NULL,
    user_id uuid NOT NULL,
    amount numeric(10,2) NOT NULL,
    currency character varying(3) NOT NULL DEFAULT 'USD',
    payment_method character varying(20) NOT NULL,
    status character varying(20) NOT NULL,
    transaction_id character varying(255),
    payment_gateway character varying(50),
    payment_identifier character varying(100),
    gateway_response jsonb NOT NULL DEFAULT '{}',
    error_message text,
    error_code character varying(50),
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    updated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    completed_at timestamp with time zone,
    refunded_at timestamp with time zone,
    refund_amount numeric(10,2),
    refund_reason text,
    ip_address character varying(45),
    user_agent character varying(500),
    metadata jsonb NOT NULL DEFAULT '{}',
    CONSTRAINT "PK_payments" PRIMARY KEY (id),
    CONSTRAINT ck_payments_amount CHECK (amount >= 0),
    CONSTRAINT ck_payments_refund_amount CHECK (refund_amount IS NULL OR (refund_amount >= 0 AND refund_amount <= amount)),
    CONSTRAINT "FK_payments_orders_order_id" FOREIGN KEY (order_id) REFERENCES orders (id) ON DELETE CASCADE,
    CONSTRAINT "FK_payments_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE RESTRICT
);

CREATE INDEX idx_game_servers_expires_at ON game_servers (expires_at);

CREATE UNIQUE INDEX idx_game_servers_ip_port ON game_servers (ip_address, port) WHERE ip_address IS NOT NULL AND port IS NOT NULL;

CREATE INDEX idx_game_servers_last_heartbeat ON game_servers (last_heartbeat_at);

CREATE INDEX idx_game_servers_server_type_id ON game_servers (server_type_id);

CREATE INDEX idx_game_servers_status ON game_servers (status);

CREATE INDEX idx_game_servers_status_expires ON game_servers (status, expires_at);

CREATE INDEX idx_game_servers_user_id ON game_servers (user_id);

CREATE UNIQUE INDEX idx_game_servers_user_server_name ON game_servers (user_id, server_name);

CREATE INDEX idx_orders_created_at ON orders (created_at);

CREATE UNIQUE INDEX idx_orders_order_number ON orders (order_number);

CREATE INDEX idx_orders_payment_date ON orders (payment_date);

CREATE INDEX idx_orders_server_id ON orders (server_id);

CREATE INDEX idx_orders_server_plan_id ON orders (server_plan_id);

CREATE INDEX idx_orders_status ON orders (status);

CREATE INDEX idx_orders_user_created ON orders (user_id, created_at);

CREATE INDEX idx_orders_user_id ON orders (user_id);

CREATE INDEX idx_orders_user_status ON orders (user_id, status);

CREATE INDEX idx_payments_completed_at ON payments (completed_at);

CREATE INDEX idx_payments_created_at ON payments (created_at);

CREATE INDEX idx_payments_order_id ON payments (order_id);

CREATE INDEX idx_payments_order_status ON payments (order_id, status);

CREATE INDEX idx_payments_refunded_at ON payments (refunded_at);

CREATE INDEX idx_payments_status ON payments (status);

CREATE INDEX idx_payments_transaction_id ON payments (transaction_id);

CREATE INDEX idx_payments_user_id ON payments (user_id);

CREATE INDEX idx_payments_user_status ON payments (user_id, status);

CREATE INDEX idx_server_plans_display_order ON server_plans (display_order);

CREATE INDEX idx_server_plans_is_active ON server_plans (is_active);

CREATE INDEX idx_server_plans_server_type_id ON server_plans (server_type_id);

CREATE INDEX idx_server_plans_type_active_order ON server_plans (server_type_id, is_active, display_order);

CREATE UNIQUE INDEX idx_server_plans_type_cycle ON server_plans (server_type_id, billing_cycle);

CREATE INDEX idx_server_types_game_active ON server_types (game_name, is_active);

CREATE INDEX idx_server_types_game_name ON server_types (game_name);

CREATE INDEX idx_server_types_is_active ON server_types (is_active);

CREATE UNIQUE INDEX idx_server_types_name ON server_types (name);

CREATE INDEX idx_users_created_at ON users (created_at);

CREATE UNIQUE INDEX idx_users_email ON users (email);

CREATE INDEX idx_users_is_active ON users (is_active);

CREATE INDEX idx_users_role ON users (role);

CREATE UNIQUE INDEX idx_users_username ON users (username);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251013232951_EnhancedEntityRelationships', '8.0.11');

COMMIT;


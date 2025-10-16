-- =====================================================
-- Game Server Hosting Platform - PostgreSQL Schema
-- =====================================================
-- Version: 1.0
-- Database: PostgreSQL 14+
-- Description: Complete schema for game server hosting platform
-- =====================================================

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =====================================================
-- ENUMS
-- =====================================================

-- User role enumeration
CREATE TYPE user_role AS ENUM ('user', 'admin', 'moderator');

-- Game server status enumeration
CREATE TYPE server_status AS ENUM ('pending', 'running', 'stopped', 'suspended', 'terminated');

-- Order type enumeration
CREATE TYPE order_type AS ENUM ('new_server', 'renewal', 'upgrade');

-- Order status enumeration
CREATE TYPE order_status AS ENUM ('pending', 'completed', 'failed', 'refunded');

-- Payment method enumeration
CREATE TYPE payment_method AS ENUM ('credit_card', 'paypal', 'stripe', 'crypto');

-- =====================================================
-- TABLE: Users
-- =====================================================

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) NOT NULL UNIQUE,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone_number VARCHAR(20),
    role user_role NOT NULL DEFAULT 'user',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP,
    
    CONSTRAINT chk_username_length CHECK (LENGTH(username) >= 3),
    CONSTRAINT chk_email_format CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
);

-- Indexes for Users
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_is_active ON users(is_active);

-- =====================================================
-- TABLE: ServerTypes
-- =====================================================

CREATE TABLE server_types (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    game_name VARCHAR(100) NOT NULL,
    description TEXT NOT NULL,
    cpu_cores INTEGER NOT NULL,
    ram_mb INTEGER NOT NULL,
    disk_gb INTEGER NOT NULL,
    max_players INTEGER NOT NULL,
    hourly_price DECIMAL(10, 2) NOT NULL,
    monthly_price DECIMAL(10, 2) NOT NULL,
    docker_image VARCHAR(255) NOT NULL,
    default_config JSONB NOT NULL DEFAULT '{}',
    default_version VARCHAR(50),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT chk_cpu_cores_positive CHECK (cpu_cores > 0),
    CONSTRAINT chk_ram_mb_positive CHECK (ram_mb > 0),
    CONSTRAINT chk_disk_gb_positive CHECK (disk_gb > 0),
    CONSTRAINT chk_max_players_positive CHECK (max_players > 0),
    CONSTRAINT chk_hourly_price_non_negative CHECK (hourly_price >= 0),
    CONSTRAINT chk_monthly_price_non_negative CHECK (monthly_price >= 0)
);

-- Indexes for ServerTypes
CREATE INDEX idx_server_types_game_name ON server_types(game_name);
CREATE INDEX idx_server_types_is_active ON server_types(is_active);

-- GIN index for JSONB default_config
CREATE INDEX idx_server_types_default_config ON server_types USING GIN (default_config);

-- =====================================================
-- TABLE: GameServers
-- =====================================================

CREATE TABLE game_servers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    server_type_id UUID NOT NULL REFERENCES server_types(id) ON DELETE RESTRICT,
    server_name VARCHAR(100) NOT NULL,
    ip_address VARCHAR(45),
    port INTEGER,
    status server_status NOT NULL DEFAULT 'pending',
    configuration JSONB NOT NULL DEFAULT '{}',
    version VARCHAR(50) NOT NULL,
    deployed_at TIMESTAMP,
    expires_at TIMESTAMP,
    last_heartbeat_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Provisioning fields
    provider_type VARCHAR(50),
    provider_id VARCHAR(100),
    provider_region VARCHAR(50),
    provisioning_started_at TIMESTAMP,
    provisioning_completed_at TIMESTAMP,
    provisioning_error TEXT,
    ssh_key_private TEXT,
    ssh_key_public TEXT,
    health_check_status VARCHAR(50),
    
    CONSTRAINT chk_port_range CHECK (port IS NULL OR (port >= 1 AND port <= 65535)),
    CONSTRAINT uq_user_server_name UNIQUE (user_id, server_name)
);

-- Indexes for GameServers
CREATE INDEX idx_game_servers_user_id ON game_servers(user_id);
CREATE INDEX idx_game_servers_server_type_id ON game_servers(server_type_id);
CREATE INDEX idx_game_servers_status ON game_servers(status);
CREATE INDEX idx_game_servers_expires_at ON game_servers(expires_at);

-- Unique index for IP and port combination (only when both are not null)
CREATE UNIQUE INDEX idx_game_servers_ip_port ON game_servers(ip_address, port) 
    WHERE ip_address IS NOT NULL AND port IS NOT NULL;

-- GIN index for JSONB configuration
CREATE INDEX idx_game_servers_configuration ON game_servers USING GIN (configuration);

-- =====================================================
-- TABLE: Orders
-- =====================================================

CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    server_id UUID REFERENCES game_servers(id) ON DELETE SET NULL,
    order_number VARCHAR(50) NOT NULL UNIQUE,
    order_type order_type NOT NULL,
    status order_status NOT NULL DEFAULT 'pending',
    total_amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    payment_method payment_method NOT NULL,
    payment_transaction_id VARCHAR(255),
    payment_date TIMESTAMP,
    billing_details JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT chk_total_amount_non_negative CHECK (total_amount >= 0),
    CONSTRAINT chk_currency_code CHECK (LENGTH(currency) = 3)
);

-- Indexes for Orders
CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_orders_server_id ON orders(server_id);
CREATE INDEX idx_orders_order_number ON orders(order_number);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_created_at ON orders(created_at DESC);
CREATE INDEX idx_orders_payment_date ON orders(payment_date);

-- GIN index for JSONB billing_details
CREATE INDEX idx_orders_billing_details ON orders USING GIN (billing_details);

-- =====================================================
-- TRIGGERS FOR UPDATED_AT
-- =====================================================

-- Function to automatically update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply trigger to all tables
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_server_types_updated_at BEFORE UPDATE ON server_types
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_game_servers_updated_at BEFORE UPDATE ON game_servers
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_orders_updated_at BEFORE UPDATE ON orders
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- SEED DATA - Sample ServerTypes
-- =====================================================

INSERT INTO server_types (name, game_name, description, cpu_cores, ram_mb, disk_gb, max_players, hourly_price, monthly_price, docker_image, default_config) VALUES
('Minecraft Basic', 'Minecraft', 'Perfect for small communities and friends', 2, 2048, 10, 10, 0.50, 20.00, 'itzg/minecraft-server:latest', 
    '{"difficulty": "normal", "pvp": true, "max_world_size": 5000, "backup_frequency": "daily", "game_mode": "survival"}'::jsonb),
    
('Minecraft Standard', 'Minecraft', 'Great for medium-sized servers with plugins', 4, 4096, 25, 50, 1.00, 40.00, 'itzg/minecraft-server:latest', 
    '{"difficulty": "normal", "pvp": true, "max_world_size": 10000, "backup_frequency": "daily", "game_mode": "survival"}'::jsonb),
    
('Minecraft Premium', 'Minecraft', 'High-performance for large communities', 8, 8192, 50, 100, 2.00, 80.00, 'itzg/minecraft-server:latest', 
    '{"difficulty": "hard", "pvp": true, "max_world_size": 20000, "backup_frequency": "hourly", "game_mode": "survival"}'::jsonb),

('CS:GO Casual', 'Counter-Strike: Global Offensive', 'Casual 10v10 server', 2, 2048, 15, 20, 0.75, 30.00, 'cm2network/csgo:latest', 
    '{"game_type": "0", "game_mode": "0", "tickrate": 64, "mapgroup": "mg_active"}'::jsonb),
    
('CS:GO Competitive', 'Counter-Strike: Global Offensive', 'Competitive 5v5 with 128 tick', 4, 4096, 20, 10, 1.50, 60.00, 'cm2network/csgo:latest', 
    '{"game_type": "0", "game_mode": "1", "tickrate": 128, "mapgroup": "mg_active"}'::jsonb),

('Terraria Small', 'Terraria', 'Small world for friends', 1, 1024, 5, 8, 0.25, 10.00, 'ryshe/terraria:latest', 
    '{"world_size": "small", "difficulty": "normal", "max_players": 8}'::jsonb),

('ARK: Survival Evolved', 'ARK: Survival Evolved', 'Standard ARK server', 4, 6144, 30, 20, 1.75, 70.00, 'thmhoag/arkserver:latest', 
    '{"difficulty": 1.0, "taming_speed": 1.0, "harvest_amount": 1.0, "max_players": 20}'::jsonb),

('Valheim', 'Valheim', 'Viking survival server', 2, 4096, 15, 10, 0.60, 25.00, 'lloesche/valheim-server:latest', 
    '{"world_name": "Dedicated", "server_public": false, "max_players": 10}'::jsonb);

-- =====================================================
-- VIEWS
-- =====================================================

-- View for active servers with user and type information
CREATE VIEW v_active_servers AS
SELECT 
    gs.id,
    gs.server_name,
    gs.ip_address,
    gs.port,
    gs.status,
    gs.expires_at,
    u.username AS owner_username,
    u.email AS owner_email,
    st.name AS server_type_name,
    st.game_name,
    st.cpu_cores,
    st.ram_mb,
    gs.created_at
FROM game_servers gs
INNER JOIN users u ON gs.user_id = u.id
INNER JOIN server_types st ON gs.server_type_id = st.id
WHERE gs.status IN ('running', 'stopped');

-- View for order summary
CREATE VIEW v_order_summary AS
SELECT 
    o.id,
    o.order_number,
    o.order_type,
    o.status,
    o.total_amount,
    o.currency,
    o.payment_method,
    o.created_at,
    u.username,
    u.email,
    gs.server_name
FROM orders o
INNER JOIN users u ON o.user_id = u.id
LEFT JOIN game_servers gs ON o.server_id = gs.id;

-- View for server revenue analytics
CREATE VIEW v_revenue_summary AS
SELECT 
    st.game_name,
    st.name AS server_type_name,
    COUNT(DISTINCT gs.id) AS active_servers,
    SUM(o.total_amount) AS total_revenue,
    AVG(o.total_amount) AS avg_order_value
FROM server_types st
LEFT JOIN game_servers gs ON st.id = gs.server_type_id AND gs.status IN ('running', 'stopped')
LEFT JOIN orders o ON gs.id = o.server_id AND o.status = 'completed'
GROUP BY st.id, st.game_name, st.name;

-- =====================================================
-- UTILITY FUNCTIONS
-- =====================================================

-- Function to generate unique order number
CREATE OR REPLACE FUNCTION generate_order_number()
RETURNS VARCHAR(50) AS $$
DECLARE
    date_prefix VARCHAR(8);
    seq_num INTEGER;
    order_num VARCHAR(50);
BEGIN
    -- Format: ORD-YYYYMMDD-NNNNN
    date_prefix := TO_CHAR(CURRENT_DATE, 'YYYYMMDD');
    
    -- Get next sequence number for today
    SELECT COALESCE(MAX(
        CAST(SUBSTRING(order_number FROM 14) AS INTEGER)
    ), 0) + 1
    INTO seq_num
    FROM orders
    WHERE order_number LIKE 'ORD-' || date_prefix || '-%';
    
    order_num := 'ORD-' || date_prefix || '-' || LPAD(seq_num::TEXT, 5, '0');
    
    RETURN order_num;
END;
$$ LANGUAGE plpgsql;

-- Function to check server expiration
CREATE OR REPLACE FUNCTION check_server_expiration()
RETURNS VOID AS $$
BEGIN
    -- Suspend servers that have expired
    UPDATE game_servers
    SET status = 'suspended'
    WHERE expires_at < CURRENT_TIMESTAMP
    AND status IN ('running', 'stopped');
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- COMMENTS
-- =====================================================

COMMENT ON TABLE users IS 'User accounts and authentication';
COMMENT ON TABLE server_types IS 'Available game server types and pricing';
COMMENT ON TABLE game_servers IS 'Individual game server instances';
COMMENT ON TABLE orders IS 'Order and payment transactions';

COMMENT ON COLUMN users.password_hash IS 'Bcrypt hashed password';
COMMENT ON COLUMN server_types.default_config IS 'JSON default configuration for server type';
COMMENT ON COLUMN game_servers.configuration IS 'JSON server-specific configuration';
COMMENT ON COLUMN orders.billing_details IS 'JSON billing address and information';

-- =====================================================
-- END OF SCHEMA
-- =====================================================

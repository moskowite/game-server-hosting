-- Migration: Add Server Provisioning Fields
-- Date: 2025-10-14
-- Description: Adds provisioning-related fields to game_servers table

-- Add provisioning fields to game_servers table
ALTER TABLE game_servers 
ADD COLUMN provider_type VARCHAR(50),
ADD COLUMN provider_id VARCHAR(255),
ADD COLUMN provider_region VARCHAR(50),
ADD COLUMN provisioning_started_at TIMESTAMP,
ADD COLUMN provisioning_completed_at TIMESTAMP,
ADD COLUMN provisioning_error TEXT,
ADD COLUMN ssh_key_private TEXT,
ADD COLUMN ssh_key_public VARCHAR(500),
ADD COLUMN health_check_status VARCHAR(20);

-- Create indexes for better query performance
CREATE INDEX idx_game_servers_provider_id ON game_servers(provider_id);
CREATE INDEX idx_game_servers_provider_type ON game_servers(provider_type);
CREATE INDEX idx_game_servers_status ON game_servers(status);
CREATE INDEX idx_game_servers_health_check ON game_servers(last_heartbeat_at);

-- Add comments to document the fields
COMMENT ON COLUMN game_servers.provider_type IS 'Cloud provider type (e.g., DigitalOcean, AWS, Azure)';
COMMENT ON COLUMN game_servers.provider_id IS 'Cloud provider''s server identifier (e.g., Droplet ID)';
COMMENT ON COLUMN game_servers.provider_region IS 'Cloud provider region/datacenter';
COMMENT ON COLUMN game_servers.provisioning_started_at IS 'When provisioning started';
COMMENT ON COLUMN game_servers.provisioning_completed_at IS 'When provisioning completed';
COMMENT ON COLUMN game_servers.provisioning_error IS 'Error message if provisioning failed';
COMMENT ON COLUMN game_servers.ssh_key_private IS 'SSH private key for server management (encrypted)';
COMMENT ON COLUMN game_servers.ssh_key_public IS 'SSH public key fingerprint';
COMMENT ON COLUMN game_servers.health_check_status IS 'Health check status (healthy, unhealthy)';

# Entity Relationships Migration Guide

This guide explains how to apply the entity relationships migration to your database.

## Prerequisites

- .NET 8.0 SDK installed
- Entity Framework Core tools installed: `dotnet tool install --global dotnet-ef`
- PostgreSQL database server running
- Connection string configured in `appsettings.json`

## Migration Details

**Migration Name**: `EnhancedEntityRelationships`  
**Migration File**: `src/GameServerHosting.Api/Data/Migrations/20251013232951_EnhancedEntityRelationships.cs`  
**Created**: 2025-10-13

## What's Included

This migration creates the complete database schema with:

### ✅ Tables Created
- `users` - User accounts and profiles
- `server_types` - Available game server types
- `server_plans` - Billing plans for each server type
- `game_servers` - Individual server instances
- `orders` - Purchase orders
- `payments` - Payment transactions

### ✅ Foreign Key Relationships
- User → GameServers (CASCADE)
- User → Orders (RESTRICT)
- User → Payments (RESTRICT)
- ServerType → GameServers (RESTRICT)
- ServerType → ServerPlans (RESTRICT)
- GameServer → Orders (SET NULL)
- ServerPlan → Orders (RESTRICT)
- Order → Payments (CASCADE)

### ✅ Indexes (36 total)
- Unique indexes for natural keys (email, username, order_number)
- Foreign key indexes for join optimization
- Composite indexes for common query patterns
- Filtered index for IP/port combinations

### ✅ Check Constraints (12 total)
- Price validation (>= 0)
- Resource validation (cpu, ram, disk > 0)
- Port range validation (1-65535)
- Discount percentage validation (0-100)
- Refund amount validation

## Applying the Migration

### Option 1: Using Default Connection String

```bash
cd src/GameServerHosting.Api
dotnet ef database update
```

### Option 2: Using Custom Connection String

```bash
cd src/GameServerHosting.Api
dotnet ef database update --connection "Host=localhost;Database=gameserver;Username=postgres;Password=yourpassword"
```

### Option 3: Using Environment-Specific Settings

```bash
cd src/GameServerHosting.Api

# Development
dotnet ef database update --environment Development

# Staging
dotnet ef database update --environment Staging

# Production
dotnet ef database update --environment Production
```

## Verifying the Migration

After applying the migration, verify it was successful:

### 1. Check Migration History

```bash
dotnet ef migrations list
```

You should see:
```
20251013232951_EnhancedEntityRelationships (Applied)
```

### 2. Check Database Tables

Connect to your PostgreSQL database and run:

```sql
-- List all tables
\dt

-- Check table structure
\d users
\d server_types
\d game_servers
\d orders
\d payments
\d server_plans

-- View foreign keys
SELECT
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name,
    rc.delete_rule
FROM
    information_schema.table_constraints AS tc
    JOIN information_schema.key_column_usage AS kcu
      ON tc.constraint_name = kcu.constraint_name
    JOIN information_schema.constraint_column_usage AS ccu
      ON ccu.constraint_name = tc.constraint_name
    JOIN information_schema.referential_constraints AS rc
      ON rc.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY';

-- View check constraints
SELECT
    con.conname AS constraint_name,
    tbl.relname AS table_name,
    pg_get_constraintdef(con.oid) AS constraint_definition
FROM
    pg_constraint con
    JOIN pg_class tbl ON tbl.oid = con.conrelid
WHERE
    con.contype = 'c';

-- View indexes
SELECT
    indexname,
    tablename,
    indexdef
FROM
    pg_indexes
WHERE
    schemaname = 'public'
ORDER BY
    tablename,
    indexname;
```

## Testing the Relationships

### Test Cascade Delete (User → GameServers)

```sql
-- Insert test data
INSERT INTO users (id, email, username, password_hash, first_name, last_name, role, created_at, updated_at)
VALUES (gen_random_uuid(), 'test@example.com', 'testuser', 'hash', 'Test', 'User', 'User', NOW(), NOW())
RETURNING id;

-- Note the returned user ID, then insert a game server
INSERT INTO server_types (id, name, game_name, description, cpu_cores, ram_mb, disk_gb, max_players, hourly_price, monthly_price, docker_image, created_at, updated_at)
VALUES (gen_random_uuid(), 'Test Type', 'TestGame', 'Test', 2, 2048, 50, 10, 1.00, 20.00, 'test:latest', NOW(), NOW())
RETURNING id;

-- Insert game server linked to user
INSERT INTO game_servers (id, user_id, server_type_id, server_name, status, version, created_at, updated_at)
VALUES (gen_random_uuid(), 'USER_ID_HERE', 'SERVER_TYPE_ID_HERE', 'Test Server', 'Pending', '1.0', NOW(), NOW());

-- Delete user - should cascade delete the game server
DELETE FROM users WHERE email = 'test@example.com';

-- Verify game server was deleted
SELECT COUNT(*) FROM game_servers WHERE server_name = 'Test Server';
-- Should return 0
```

### Test Restrict Delete (User → Orders)

```sql
-- Insert user
INSERT INTO users (id, email, username, password_hash, first_name, last_name, role, created_at, updated_at)
VALUES (gen_random_uuid(), 'test2@example.com', 'testuser2', 'hash', 'Test', 'User', 'User', NOW(), NOW())
RETURNING id;

-- Insert order for user
INSERT INTO orders (id, user_id, order_number, order_type, status, total_amount, payment_method, created_at, updated_at)
VALUES (gen_random_uuid(), 'USER_ID_HERE', 'ORD-TEST-001', 'NewServer', 'Pending', 50.00, 'CreditCard', NOW(), NOW());

-- Try to delete user - should fail with foreign key constraint error
DELETE FROM users WHERE email = 'test2@example.com';
-- Expected: ERROR: update or delete on table "users" violates foreign key constraint
```

### Test Check Constraints

```sql
-- Try to insert server type with negative price (should fail)
INSERT INTO server_types (id, name, game_name, description, cpu_cores, ram_mb, disk_gb, max_players, hourly_price, monthly_price, docker_image, created_at, updated_at)
VALUES (gen_random_uuid(), 'Invalid Type', 'TestGame', 'Test', 2, 2048, 50, 10, -1.00, 20.00, 'test:latest', NOW(), NOW());
-- Expected: ERROR: new row for relation "server_types" violates check constraint "ck_server_types_hourly_price"

-- Try to insert game server with invalid port (should fail)
INSERT INTO game_servers (id, user_id, server_type_id, server_name, port, status, version, created_at, updated_at)
VALUES (gen_random_uuid(), 'USER_ID_HERE', 'SERVER_TYPE_ID_HERE', 'Test Server', 99999, 'Pending', '1.0', NOW(), NOW());
-- Expected: ERROR: new row for relation "game_servers" violates check constraint "ck_game_servers_port"
```

## Rolling Back

If you need to roll back this migration:

```bash
cd src/GameServerHosting.Api
dotnet ef database update 0
```

This will drop all tables created by this migration.

## Troubleshooting

### Issue: "Unable to create a 'DbContext'"

**Solution**: Ensure your connection string is properly configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gameserver;Username=postgres;Password=yourpassword"
  }
}
```

### Issue: "Database does not exist"

**Solution**: Create the database first:

```sql
CREATE DATABASE gameserver;
```

### Issue: "Role does not exist"

**Solution**: Create the PostgreSQL user:

```sql
CREATE USER postgres WITH PASSWORD 'yourpassword';
ALTER USER postgres WITH SUPERUSER;
```

### Issue: "uuid_generate_v4() function does not exist"

**Solution**: Enable the uuid-ossp extension:

```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
```

Or use `gen_random_uuid()` which is built-in to PostgreSQL 13+.

## Next Steps

After successfully applying this migration:

1. **Seed Initial Data**: Create seed data for server types and plans
2. **Test Relationships**: Verify all foreign keys and cascade behaviors work
3. **Performance Test**: Run query performance tests to validate indexes
4. **Implement Repositories**: Create repository classes for data access
5. **Add Business Logic**: Implement services using the configured relationships

## Additional Commands

### Generate SQL Script (without applying)

```bash
dotnet ef migrations script --output schema.sql
```

### View Migration Status

```bash
dotnet ef migrations list
```

### Remove Last Migration (if not applied)

```bash
dotnet ef migrations remove
```

### Create New Migration

```bash
dotnet ef migrations add MigrationName --output-dir Data/Migrations
```

## Support

For issues or questions:
1. Check the Entity Framework Core documentation: https://docs.microsoft.com/ef/core/
2. Review the PostgreSQL documentation: https://www.postgresql.org/docs/
3. Consult the ENTITY_RELATIONSHIPS_SUMMARY.md for detailed relationship information

---

**Migration Created**: 2025-10-13  
**EF Core Version**: 8.0.11  
**PostgreSQL Provider**: Npgsql 8.0.11  
**Target Framework**: .NET 8.0

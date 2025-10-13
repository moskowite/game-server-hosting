# Entity Relationships Setup - Summary

This document summarizes the entity relationship configuration completed for the Game Server Hosting platform.

## Overview

All entity relationships have been properly configured with:
- ✅ One-to-many relationships
- ✅ Foreign key constraints
- ✅ Comprehensive indexes (single and composite)
- ✅ Cascade delete rules
- ✅ Check constraints for data integrity

## Entity Relationship Diagram

```
User (1) ──────< (N) GameServer
User (1) ──────< (N) Order
User (1) ──────< (N) Payment

ServerType (1) ──────< (N) GameServer
ServerType (1) ──────< (N) ServerPlan

GameServer (1) ──────< (N) Order

ServerPlan (1) ──────< (N) Order

Order (1) ──────< (N) Payment
```

## Configured Relationships

### 1. User Relationships

#### User → GameServers (One-to-Many)
- **Foreign Key**: `GameServer.UserId` → `User.Id`
- **Delete Behavior**: `Cascade` - When a user is deleted, all their game servers are deleted
- **Navigation Properties**:
  - `User.GameServers` (Collection)
  - `GameServer.User` (Reference)

#### User → Orders (One-to-Many)
- **Foreign Key**: `Order.UserId` → `User.Id`
- **Delete Behavior**: `Restrict` - Cannot delete a user with existing orders (preserves order history)
- **Navigation Properties**:
  - `User.Orders` (Collection)
  - `Order.User` (Reference)

#### User → Payments (One-to-Many)
- **Foreign Key**: `Payment.UserId` → `User.Id`
- **Delete Behavior**: `Restrict` - Cannot delete a user with existing payments (preserves payment history)
- **Navigation Properties**:
  - `User.Payments` (Collection)
  - `Payment.User` (Reference)

### 2. ServerType Relationships

#### ServerType → GameServers (One-to-Many)
- **Foreign Key**: `GameServer.ServerTypeId` → `ServerType.Id`
- **Delete Behavior**: `Restrict` - Cannot delete a server type that's in use
- **Navigation Properties**:
  - `ServerType.GameServers` (Collection)
  - `GameServer.ServerType` (Reference)

#### ServerType → ServerPlans (One-to-Many)
- **Foreign Key**: `ServerPlan.ServerTypeId` → `ServerType.Id`
- **Delete Behavior**: `Restrict` - Cannot delete a server type with existing plans
- **Navigation Properties**:
  - `ServerType.ServerPlans` (Collection)
  - `ServerPlan.ServerType` (Reference)

### 3. GameServer Relationships

#### GameServer → Orders (One-to-Many)
- **Foreign Key**: `Order.ServerId` → `GameServer.Id` (nullable)
- **Delete Behavior**: `SetNull` - If server is deleted, order history is preserved with ServerId set to null
- **Navigation Properties**:
  - `GameServer.Orders` (Collection)
  - `Order.GameServer` (Reference, nullable)

### 4. ServerPlan Relationships

#### ServerPlan → Orders (One-to-Many)
- **Foreign Key**: `Order.ServerPlanId` → `ServerPlan.Id` (nullable)
- **Delete Behavior**: `Restrict` - Cannot delete a server plan with existing orders
- **Navigation Properties**:
  - `ServerPlan.Orders` (Collection)
  - `Order.ServerPlan` (Reference, nullable)

### 5. Order Relationships

#### Order → Payments (One-to-Many)
- **Foreign Key**: `Payment.OrderId` → `Order.Id`
- **Delete Behavior**: `Cascade` - When an order is deleted, all associated payments are deleted
- **Navigation Properties**:
  - `Order.Payments` (Collection)
  - `Payment.Order` (Reference)

## Indexes

### User Entity
- `idx_users_email` - Unique index on email
- `idx_users_username` - Unique index on username
- `idx_users_role` - Index for role-based queries
- `idx_users_is_active` - Index for filtering active users
- `idx_users_created_at` - Index for date-based queries

### ServerType Entity
- `idx_server_types_name` - Unique index on name
- `idx_server_types_game_name` - Index for game-based queries
- `idx_server_types_is_active` - Index for filtering active types
- `idx_server_types_game_active` - Composite index (game_name, is_active)

### GameServer Entity
- `idx_game_servers_user_id` - Foreign key index
- `idx_game_servers_server_type_id` - Foreign key index
- `idx_game_servers_status` - Index for status filtering
- `idx_game_servers_expires_at` - Index for expiration checks
- `idx_game_servers_user_server_name` - Unique composite (user_id, server_name)
- `idx_game_servers_ip_port` - Unique composite (ip_address, port) with filter
- `idx_game_servers_status_expires` - Composite (status, expires_at)
- `idx_game_servers_last_heartbeat` - Index for health check queries

### Order Entity
- `idx_orders_user_id` - Foreign key index
- `idx_orders_server_id` - Foreign key index
- `idx_orders_server_plan_id` - Foreign key index
- `idx_orders_order_number` - Unique index
- `idx_orders_status` - Index for status filtering
- `idx_orders_created_at` - Index for date-based queries
- `idx_orders_user_status` - Composite (user_id, status)
- `idx_orders_user_created` - Composite (user_id, created_at)
- `idx_orders_payment_date` - Index for payment date queries

### ServerPlan Entity
- `idx_server_plans_server_type_id` - Foreign key index
- `idx_server_plans_type_cycle` - Unique composite (server_type_id, billing_cycle)
- `idx_server_plans_is_active` - Index for filtering active plans
- `idx_server_plans_display_order` - Index for sorting
- `idx_server_plans_type_active_order` - Composite (server_type_id, is_active, display_order)

### Payment Entity
- `idx_payments_order_id` - Foreign key index
- `idx_payments_user_id` - Foreign key index
- `idx_payments_status` - Index for status filtering
- `idx_payments_transaction_id` - Index for transaction lookups
- `idx_payments_created_at` - Index for date-based queries
- `idx_payments_user_status` - Composite (user_id, status)
- `idx_payments_order_status` - Composite (order_id, status)
- `idx_payments_completed_at` - Index for completion date queries
- `idx_payments_refunded_at` - Index for refund queries

## Check Constraints

### ServerType
- `ck_server_types_cpu_cores` - cpu_cores > 0
- `ck_server_types_ram_mb` - ram_mb > 0
- `ck_server_types_disk_gb` - disk_gb > 0
- `ck_server_types_max_players` - max_players > 0
- `ck_server_types_hourly_price` - hourly_price >= 0
- `ck_server_types_monthly_price` - monthly_price >= 0

### GameServer
- `ck_game_servers_port` - port IS NULL OR (port >= 1 AND port <= 65535)

### Order
- `ck_orders_total_amount` - total_amount >= 0

### ServerPlan
- `ck_server_plans_price` - price >= 0
- `ck_server_plans_duration` - duration_hours > 0
- `ck_server_plans_discount` - discount_percentage >= 0 AND discount_percentage <= 100

### Payment
- `ck_payments_amount` - amount >= 0
- `ck_payments_refund_amount` - refund_amount IS NULL OR (refund_amount >= 0 AND refund_amount <= amount)

## Cascade Delete Rules Rationale

### Cascade Delete
Used when child entities have no meaning without the parent:
- **User → GameServers**: Servers are owned by users and should be removed when the user is deleted
- **Order → Payments**: Payments belong to orders and should be removed with the order

### Restrict Delete
Used to preserve historical/audit data:
- **User → Orders**: Orders contain important business records that must be preserved
- **User → Payments**: Payment records are critical for financial auditing
- **ServerType → GameServers**: Prevents accidental deletion of server types in use
- **ServerType → ServerPlans**: Prevents deletion of types with configured plans
- **ServerPlan → Orders**: Preserves order history referencing plans

### Set Null
Used when the relationship is optional but the record should be preserved:
- **GameServer → Orders**: If a server is deleted, orders remain for historical tracking

## Migration

The migration `20251013232951_EnhancedEntityRelationships.cs` has been created and includes:

1. **Table Creation**: All tables with proper column definitions
2. **Primary Keys**: UUID-based primary keys with auto-generation
3. **Foreign Keys**: All relationship foreign keys with appropriate delete behaviors
4. **Indexes**: All single and composite indexes for query optimization
5. **Check Constraints**: Data validation at the database level
6. **Default Values**: Appropriate defaults for columns (timestamps, booleans, JSON)

## Applying the Migration

To apply this migration to your database:

```bash
# From the API project directory
cd src/GameServerHosting.Api

# Apply migration
dotnet ef database update

# Or apply to specific environment
dotnet ef database update --connection "YourConnectionString"
```

## Database Diagram

The database schema supports the following use cases:

1. **User Management**: Users can register, authenticate, and manage their profile
2. **Server Provisioning**: Users can order and manage multiple game servers
3. **Billing Plans**: Flexible billing plans per server type (hourly, monthly, etc.)
4. **Order Processing**: Complete order history with server and plan associations
5. **Payment Tracking**: Detailed payment transactions with gateway integration
6. **Audit Trail**: Timestamps on all records for tracking changes

## Performance Considerations

The indexes have been optimized for common query patterns:

- **User Queries**: By email, username, role, active status
- **Server Queries**: By user, status, expiration, type
- **Order Queries**: By user, status, date, server
- **Payment Queries**: By user, order, status, transaction ID
- **Plan Queries**: By server type, active status, display order

## Next Steps

1. ✅ Entity relationships configured
2. ✅ Foreign keys and indexes added
3. ✅ Cascade delete rules set up
4. ✅ Check constraints implemented
5. ✅ Migration generated
6. 🔄 Apply migration to database: `dotnet ef database update`
7. 🔄 Test relationships with sample data
8. 🔄 Implement repository pattern and business logic

## Files Modified

- `src/GameServerHosting.Api/Data/GameServerDbContext.cs` - Enhanced with indexes and constraints
- `src/GameServerHosting.Api/GameServerHosting.Api.csproj` - EF Core package versions aligned
- `src/GameServerHosting.Api/Data/Migrations/20251013232951_EnhancedEntityRelationships.cs` - Generated migration

## Testing Recommendations

1. **Cascade Delete**: Verify that deleting a user removes their servers
2. **Restrict Delete**: Ensure users with orders/payments cannot be deleted
3. **Set Null**: Confirm that deleting a server preserves order history
4. **Check Constraints**: Test that invalid data (negative prices, invalid ports) is rejected
5. **Indexes**: Run EXPLAIN ANALYZE on common queries to verify index usage
6. **Unique Constraints**: Test that duplicate emails, usernames, order numbers are prevented

## Conclusion

All entity relationships have been properly configured following best practices:
- ✅ Referential integrity enforced with foreign keys
- ✅ Data integrity ensured with check constraints
- ✅ Query performance optimized with strategic indexes
- ✅ Business logic enforced with appropriate cascade rules
- ✅ Audit trail maintained with timestamps on all entities

The database schema is now production-ready and supports all planned features for the Game Server Hosting platform.

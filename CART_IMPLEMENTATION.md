# Shopping Cart Implementation

This document describes the shopping cart feature implementation for the Game Server Hosting platform.

## Overview

The shopping cart feature allows authenticated users to:
- Add server plans to their cart
- View their cart items
- Update item quantities
- Remove items from cart
- Clear their entire cart

## Implementation Details

### Entity Model

**File:** `src/GameServerHosting.Shared/Entities/CartItem.cs`

The `CartItem` entity includes:
- `Id` - Unique identifier
- `UserId` - Reference to the user who owns the cart item
- `ServerPlanId` - Reference to the server plan being purchased
- `Quantity` - Number of items (must be >= 1)
- `Price` - Price at the time item was added (captured for price consistency)
- `Currency` - Currency code (ISO 4217)
- `CreatedAt` / `UpdatedAt` - Timestamps

### Database Configuration

**File:** `src/GameServerHosting.Api/Data/GameServerDbContext.cs`

- Added `DbSet<CartItem> CartItems` property
- Configured entity mapping with:
  - Table name: `cart_items`
  - Check constraints on quantity (> 0) and price (>= 0)
  - Indexes on UserId, ServerPlanId, and CreatedAt
  - Unique constraint on (UserId, ServerPlanId) to prevent duplicate cart items
  - Foreign key relationships with cascade delete

### API Endpoints

**File:** `src/GameServerHosting.Api/Controllers/CartController.cs`

All endpoints require authentication (`[Authorize]` attribute).

#### 1. GET /api/cart
- Returns all cart items for the authenticated user
- Includes server plan details with each cart item
- Response: `200 OK` with array of `CartItemDto`

#### 2. POST /api/cart/add
- Adds a new item to the cart or updates quantity if item already exists
- Request body: `AddToCartRequest` (ServerPlanId, Quantity)
- Validates that the server plan exists and is active
- Updates price to current server plan price
- Response: `200 OK` with `CartItemDto`

#### 3. PUT /api/cart/update/{itemId}
- Updates the quantity of a specific cart item
- Request body: `UpdateCartItemRequest` (Quantity)
- Validates that the user owns the cart item
- Response: `200 OK` with updated `CartItemDto`

#### 4. DELETE /api/cart/remove/{itemId}
- Removes a specific item from the cart
- Validates that the user owns the cart item
- Response: `200 OK` with success message

#### 5. DELETE /api/cart/clear
- Removes all items from the authenticated user's cart
- Response: `200 OK` with count of items removed

### DTOs and Request Models

**Files in:** `src/GameServerHosting.Api/Models/`

1. **CartItemDto.cs** - Data transfer object for cart items
   - Includes all cart item properties
   - Includes nested `ServerPlanDto` for plan details

2. **AddToCartRequest.cs** - Request model for adding items
   - `ServerPlanId` (required)
   - `Quantity` (required, min: 1)

3. **UpdateCartItemRequest.cs** - Request model for updating quantity
   - `Quantity` (required, min: 1)

### Testing

**File:** `src/GameServerHosting.Api/cart-tests.http`

HTTP test file with examples for all endpoints. Update the following before testing:
- `@token` - Your JWT authentication token
- Server plan IDs and cart item IDs as needed

## Database Migration

To apply the database changes, run the following commands:

```bash
cd src/GameServerHosting.Api
dotnet ef migrations add AddCartItemEntity
dotnet ef database update
```

This will:
1. Create a new migration file for the CartItem table
2. Apply the migration to the database

## Security Features

- All endpoints require authentication
- Users can only view and modify their own cart items
- Server plan availability is validated when adding to cart
- Ownership validation on update and delete operations

## Business Logic

1. **Adding to Cart:**
   - If item already exists, quantity is increased
   - Price is always updated to current server plan price
   - Only active server plans can be added

2. **Unique Constraint:**
   - A user cannot have duplicate cart items for the same server plan
   - This is enforced at the database level

3. **Price Capture:**
   - The current price is stored when adding/updating items
   - This ensures price consistency even if plan prices change

## Error Handling

All endpoints include comprehensive error handling:
- 400 Bad Request - Invalid input or business rule violations
- 401 Unauthorized - Missing or invalid authentication
- 403 Forbidden - Attempting to access/modify another user's cart
- 404 Not Found - Cart item or server plan not found
- 500 Internal Server Error - Unexpected errors (logged)

## Logging

All operations are logged using `ILogger<CartController>`:
- Information level: Successful operations
- Warning level: Business rule violations, not found scenarios
- Error level: Exceptions and errors

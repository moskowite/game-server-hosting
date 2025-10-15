# Repository & Unit of Work Pattern Implementation

## Summary

This document outlines the implementation of the Repository and Unit of Work (UoW) patterns to encapsulate Entity Framework Core data access behind interfaces. This improves testability, enables clearer transaction boundaries, and prepares the codebase for future growth.

## What Was Implemented

### 1. Repository Interfaces (Application Layer)

Created comprehensive repository interfaces in `src/GameServerHosting.Application/Interfaces/`:

- **`IRepository<T>`**: Generic base repository interface with common CRUD operations
  - `GetByIdAsync()`, `GetAllAsync()`, `FindAsync()`, `FirstOrDefaultAsync()`
  - `AnyAsync()`, `CountAsync()`
  - `AddAsync()`, `AddRangeAsync()`, `Update()`, `UpdateRange()`
  - `Remove()`, `RemoveRange()`
  - `AsQueryable()` (for advanced scenarios)

- **Entity-Specific Repositories**:
  - `IUserRepository`: User-specific queries (by email, username, role, with preferences/servers)
  - `IServerTypeRepository`: Server type queries (by name, game name, active, with plans)
  - `IServerPlanRepository`: Server plan queries (by type, billing cycle, with server type, name existence check)
  - `IGameServerRepository`: Game server queries (by user, status, expired servers, stale heartbeats, with details)
  - `IOrderRepository`: Order queries (by user, status, order number, payment transaction, today's count)
  - `IPaymentRepository`: Payment queries (by user, order, transaction ID, status, with details)
  - `ICartItemRepository`: Cart queries (by user, with details, cart total, clear cart)

### 2. Unit of Work Interface (Application Layer)

Created `IUnitOfWork` interface in `src/GameServerHosting.Application/Interfaces/IUnitOfWork.cs`:

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IServerTypeRepository ServerTypes { get; }
    IServerPlanRepository ServerPlans { get; }
    IGameServerRepository GameServers { get; }
    IOrderRepository Orders { get; }
    IPaymentRepository Payments { get; }
    ICartItemRepository CartItems { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

### 3. Repository Implementations (Infrastructure Layer)

Created repository implementations in `src/GameServerHosting.Infrastructure/Repositories/`:

- **`Repository<T>`**: Base generic repository implementation
- **Specific Repositories**:
  - `UserRepository`
  - `ServerTypeRepository`
  - `ServerPlanRepository`
  - `GameServerRepository`
  - `OrderRepository`
  - `PaymentRepository`
  - `CartItemRepository`

Each repository implements its interface and includes:
- All generic repository methods from `Repository<T>`
- Entity-specific query methods with proper includes and ordering
- Optimized queries using EF Core best practices

### 4. Unit of Work Implementation (Infrastructure Layer)

Created `UnitOfWork` class in `src/GameServerHosting.Infrastructure/Repositories/UnitOfWork.cs`:

- Lazy initialization of repositories
- Transaction management support
- Proper disposal pattern
- Single `SaveChangesAsync()` for all changes

### 5. Dependency Injection Registration

Updated `src/GameServerHosting.Api/Program.cs` to register UnitOfWork:

```csharp
builder.Services.AddScoped<IUnitOfWork, GameServerHosting.Infrastructure.Repositories.UnitOfWork>();
```

### 6. Refactored Controllers & Services

**Completed Refactoring:**
- ✅ `ServerPlansController`: Fully refactored to use `IUnitOfWork`
- ✅ `GameServersController`: Fully refactored to use `IUnitOfWork`
- ✅ `PaymentService`: Fully refactored to use `IUnitOfWork`

**Partially Refactored:**
- `OrdersController`: Constructor updated, methods need completion

**Pending Refactoring:**
The following files still reference `GameServerDbContext` directly and need to be refactored:

**Controllers:**
- `src/GameServerHosting.Api/Controllers/UsersController.cs`
- `src/GameServerHosting.Api/Controllers/ServerTypesController.cs`
- `src/GameServerHosting.Api/Controllers/OrdersController.cs` (partial)
- `src/GameServerHosting.Api/Controllers/DashboardController.cs`
- `src/GameServerHosting.Api/Controllers/CartController.cs`

**Services:**
- `src/GameServerHosting.Infrastructure/Services/ServerStatusNotificationService.cs`
- `src/GameServerHosting.Infrastructure/Services/InvoiceService.cs`
- `src/GameServerHosting.Infrastructure/Services/ServerManagementService.cs`
- `src/GameServerHosting.Infrastructure/Services/OrderValidationService.cs`
- `src/GameServerHosting.Infrastructure/Services/StripeService.cs`
- `src/GameServerHosting.Infrastructure/Services/ServerProvisioningService.cs`

## Benefits Achieved

### 1. **Improved Testability**
- Controllers and services can now be unit tested with mocked repositories
- No need to mock EF Core's DbContext or DbSet<T>
- Test doubles can be easily created for repository interfaces

### 2. **Clear Separation of Concerns**
- Data access logic is now centralized in repositories
- Business logic in controllers/services doesn't need to know about EF Core
- Query complexity is encapsulated in repository methods

### 3. **Transaction Boundaries**
- Explicit transaction control through UnitOfWork
- Single `SaveChangesAsync()` call coordinates all changes
- Support for explicit transactions with `BeginTransactionAsync()`, `CommitTransactionAsync()`, `RollbackTransactionAsync()`

### 4. **Reusable Queries**
- Common queries are now reusable across the application
- Reduced code duplication (e.g., GetByUserIdAsync, GetWithDetailsAsync)
- Consistent data access patterns

### 5. **Future-Proofing**
- Easy to swap out EF Core for another ORM if needed
- Can introduce caching layers at repository level
- Supports CQRS-lite patterns (separate read/write models)
- Enables specification pattern for complex queries

## Migration Guide

### For Controllers

**Before:**
```csharp
public class MyController : ControllerBase
{
    private readonly GameServerDbContext _context;
    
    public MyController(GameServerDbContext context)
    {
        _context = context;
    }
    
    public async Task<ActionResult> Get(Guid id)
    {
        var entity = await _context.Entities
            .Include(e => e.Related)
            .FirstOrDefaultAsync(e => e.Id == id);
        return Ok(entity);
    }
}
```

**After:**
```csharp
public class MyController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    
    public MyController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<ActionResult> Get(Guid id)
    {
        var entity = await _unitOfWork.Entities.GetWithRelatedAsync(id);
        return Ok(entity);
    }
}
```

### For Services

**Before:**
```csharp
public class MyService : IMyService
{
    private readonly GameServerDbContext _context;
    
    public MyService(GameServerDbContext context)
    {
        _context = context;
    }
    
    public async Task DoSomethingAsync()
    {
        var entity = new Entity();
        _context.Entities.Add(entity);
        await _context.SaveChangesAsync();
    }
}
```

**After:**
```csharp
public class MyService : IMyService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public MyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task DoSomethingAsync()
    {
        var entity = new Entity();
        await _unitOfWork.Entities.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

## Next Steps

### 1. Complete Remaining Refactoring

Refactor the pending controllers and services listed above to use `IUnitOfWork` instead of `GameServerDbContext`.

**Priority Order:**
1. **OrdersController** - Critical for order management
2. **CartController** - Important for shopping cart functionality
3. **OrderValidationService** - Used by OrdersController
4. **ServerProvisioningService** - Core provisioning logic
5. **InvoiceService** - Order-related functionality
6. **ServerManagementService** - Server lifecycle management
7. **UsersController**, **ServerTypesController**, **DashboardController** - Lower priority
8. **StripeService**, **ServerStatusNotificationService** - Support services

### 2. Add Unit Tests

Create unit tests for repositories and services:
- Test repository methods with in-memory database or mocks
- Test service logic with mocked UnitOfWork
- Verify transaction behavior

### 3. Add Integration Tests

Create integration tests to verify:
- Repository implementations work correctly with real database
- Transaction boundaries are respected
- Concurrent operations behave correctly

### 4. Consider Advanced Patterns

Future enhancements could include:
- **Specification Pattern**: For complex query composition
- **CQRS**: Separate read and write models
- **Caching**: Add caching layer at repository level
- **Audit Logging**: Track all entity changes through UnitOfWork
- **Soft Delete**: Implement soft delete in repository base

### 5. Performance Optimization

Monitor and optimize:
- Use `AsNoTracking()` for read-only queries
- Implement projection (select only needed fields) in repositories
- Consider compiled queries for frequently-used queries
- Add database indexes based on repository query patterns

## Examples

### Example 1: Creating an Order with Transaction

```csharp
public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
{
    await _unitOfWork.BeginTransactionAsync();
    try
    {
        // Validate plan
        var plan = await _unitOfWork.ServerPlans.GetWithServerTypeAsync(request.PlanId);
        if (plan == null) throw new InvalidOperationException("Plan not found");
        
        // Create order
        var order = new Order { /* ... */ };
        await _unitOfWork.Orders.AddAsync(order);
        
        // Clear cart
        await _unitOfWork.CartItems.ClearCartAsync(userId);
        
        await _unitOfWork.CommitTransactionAsync();
        return order;
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
    }
}
```

### Example 2: Complex Query

```csharp
// In Repository
public async Task<IEnumerable<GameServer>> GetExpiredServersWithUserDetailsAsync(DateTime asOf)
{
    return await _dbSet
        .Where(gs => gs.ExpiresAt.HasValue && gs.ExpiresAt.Value <= asOf)
        .Include(gs => gs.ServerType)
        .Include(gs => gs.User)
        .OrderBy(gs => gs.ExpiresAt)
        .ToListAsync();
}

// In Service
public async Task CleanupExpiredServersAsync()
{
    var expiredServers = await _unitOfWork.GameServers
        .GetExpiredServersWithUserDetailsAsync(DateTime.UtcNow);
    
    foreach (var server in expiredServers)
    {
        // Notify user
        await _notificationService.NotifyServerExpired(server);
        
        // Terminate server
        _unitOfWork.GameServers.Remove(server);
    }
    
    await _unitOfWork.SaveChangesAsync();
}
```

## Conclusion

The Repository and Unit of Work patterns have been successfully implemented for the GameServerHosting application. The infrastructure is in place, with interfaces, implementations, and dependency injection configured. Several controllers and services have been refactored to use the new pattern.

The remaining work involves completing the refactoring of pending controllers and services, adding comprehensive tests, and potentially implementing advanced patterns like Specifications or CQRS in the future.

This implementation significantly improves the codebase's maintainability, testability, and flexibility for future enhancements.

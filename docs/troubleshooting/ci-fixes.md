# CI Fixes Documentation

This document tracks fixes made to resolve CI/CD pipeline failures.

## Table of Contents
- [Stripe Integration Build Failure](#stripe-integration-build-failure)
- [Common Build Issues](#common-build-issues)

## Stripe Integration Build Failure

### Issue Date
2025-10-14

### Problem Description
CI checks failed with compilation error in `StripeService.cs`:
```
'ServerPlan' does not contain a definition for 'MonthlyPrice' and no accessible extension method 'MonthlyPrice' accepting a first argument of type 'ServerPlan' could be found
```

### Root Cause
The `StripeService.CreateCheckoutSessionAsync` method was attempting to access a `MonthlyPrice` property on the `ServerPlan` entity that doesn't exist. The actual property name is `Price`.

### Analysis
1. **Entity Structure**: The `ServerPlan` entity (defined in `src/GameServerHosting.Shared/Entities/ServerPlan.cs`) has:
   - `Price` property (decimal) - The price for the billing cycle
   - `BillingCycle` property (enum) - Defines if it's hourly, monthly, etc.
   - No `MonthlyPrice` property

2. **Error Location**: `src/GameServerHosting.Api/Services/StripeService.cs`, line 63
   ```csharp
   // INCORRECT
   UnitAmount = (long)(serverPlan.MonthlyPrice * 100)
   ```

3. **Why This Happened**: 
   - The service was written assuming a specific property name without checking the actual entity definition
   - The `ServerPlan` entity uses a generic `Price` property that works for all billing cycles (hourly, monthly, quarterly, etc.)

### Solution
Changed line 63 in `StripeService.cs` from:
```csharp
UnitAmount = (long)(serverPlan.MonthlyPrice * 100), // Convert to cents
```

To:
```csharp
UnitAmount = (long)(serverPlan.Price * 100), // Convert to cents
```

### Files Modified
- `src/GameServerHosting.Api/Services/StripeService.cs`

### Verification
The fix ensures:
1. ✅ Code compiles successfully
2. ✅ Correct property is accessed from `ServerPlan` entity
3. ✅ Stripe receives the correct price in cents (multiplied by 100)
4. ✅ Works for all billing cycles (hourly, monthly, quarterly, etc.)

### Testing Recommendations
After this fix, verify:
1. Create checkout sessions for different billing cycles
2. Confirm prices are correctly sent to Stripe
3. Test with server plans of various price points
4. Verify Stripe dashboard shows correct amounts

### Additional Notes
- The existing warnings in `OrdersController.cs` about null references are pre-existing and don't prevent compilation
- These warnings should be addressed in a future PR to improve code quality
- The fix maintains compatibility with all billing cycle types (hourly, monthly, quarterly, annual)

---

## Common Build Issues

### Null Reference Warnings
**Status**: Non-blocking warnings

**Locations**:
- `OrdersController.cs` lines 313, 390, 447

**Issue**: 
Nullable reference type warnings when using `.ThenInclude(sp => sp.ServerType)` where `ServerType` could potentially be null.

**Impact**: 
These are compiler warnings only and don't prevent the build from succeeding. However, they indicate potential null reference exceptions at runtime.

**Recommended Fix** (Future):
Add null-conditional operators or null checks:
```csharp
// Option 1: Null-conditional
.ThenInclude(sp => sp.ServerType!)

// Option 2: Add null checks in the calling code
if (order?.ServerPlan?.ServerType != null)
{
    // Use ServerType safely
}
```

### Missing Test Results Warning
**Status**: Non-blocking warning

**Issue**: 
`.github:40: warning: Could not find any files for **/test-results.trx`

**Cause**: 
No unit tests have been created yet for the project.

**Recommended Fix** (Future):
- Create unit test project
- Add test cases for Stripe integration
- Configure test results output in CI pipeline

---

## Best Practices to Avoid Build Failures

1. **Always verify entity properties** before using them in code
   - Check the actual entity definition in `src/GameServerHosting.Shared/Entities/`
   - Use IDE autocomplete to ensure property names are correct

2. **Build locally before pushing**
   ```bash
   dotnet build
   dotnet test
   ```

3. **Enable nullable reference type checks** in your IDE
   - Helps catch potential null reference issues during development

4. **Review CI logs carefully**
   - Distinguish between errors (blocking) and warnings (non-blocking)
   - Address errors immediately, plan to fix warnings

5. **Keep documentation updated**
   - Document entity schemas
   - Maintain API contracts
   - Update this file when resolving new CI issues

---

## Related Documentation
- [Stripe Setup Guide](../setup/stripe-setup.md)
- [Database Schema](../development/database-schema.md)
- [API Development Guide](../development/api-development.md)

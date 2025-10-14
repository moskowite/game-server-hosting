# ASP.NET Core Identity Setup

## Overview
This document describes the ASP.NET Core Identity implementation for the Game Server Hosting application.

## What Was Implemented

### 1. Packages Installed

**GameServerHosting.Api:**
- **Microsoft.AspNetCore.Identity.EntityFrameworkCore** (v8.0.11)
  - Provides Identity integration with Entity Framework Core

**GameServerHosting.Shared:**
- **Microsoft.Extensions.Identity.Stores** (v8.0.11)
  - Provides the base Identity types (IdentityUser, IdentityRole, etc.)

### 2. ApplicationUser Class
Created `ApplicationUser` class that extends `IdentityUser<Guid>` with custom properties:

**Location**: `src/GameServerHosting.Shared/Entities/ApplicationUser.cs`

**Custom Properties**:
- `FirstName` - User's first name
- `LastName` - User's last name
- `Role` - Custom role enum (UserRole)
- `IsActive` - Account active status
- `CreatedAt` - Account creation timestamp
- `UpdatedAt` - Last update timestamp
- `LastLoginAt` - Last successful login timestamp

**Navigation Properties**:
- `GameServers` - Collection of game servers owned by the user
- `Orders` - Collection of orders placed by the user
- `Payments` - Collection of payment transactions

### 3. DbContext Configuration
Updated `GameServerDbContext` to inherit from `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.

**Identity Tables Configured** (PostgreSQL naming convention):
- `users` - User accounts (ApplicationUser)
- `roles` - User roles
- `user_roles` - User-role mappings
- `user_claims` - User claims
- `user_logins` - External login providers
- `user_tokens` - User authentication tokens
- `role_claims` - Role-based claims

**Column Naming**: All Identity columns use PostgreSQL snake_case convention (e.g., `user_name`, `email_confirmed`, `password_hash`).

### 4. Identity Configuration in Program.cs

#### Password Policy
- **Minimum Length**: 8 characters
- **Required Characters**:
  - At least 1 digit
  - At least 1 lowercase letter
  - At least 1 uppercase letter
  - At least 1 non-alphanumeric character
- **Unique Characters**: Minimum 4 unique characters

#### Lockout Settings
- **Lockout Duration**: 15 minutes after max failed attempts
- **Max Failed Attempts**: 5 attempts before lockout
- **Enabled for New Users**: Yes

#### User Requirements
- **Unique Email**: Required
- **Allowed Username Characters**: Letters, numbers, and special characters (-, ., _, @, +)

#### Sign-In Settings
- **Email Confirmation**: Not required (can be enabled later)
- **Phone Confirmation**: Not required
- **Account Confirmation**: Not required

#### Cookie Configuration
- **HttpOnly**: Enabled (prevents XSS attacks)
- **Secure Policy**: Always (HTTPS only)
- **SameSite**: Strict (CSRF protection)
- **Expiration**: 24 hours
- **Sliding Expiration**: Enabled
- **Auth Endpoints**:
  - Login: `/api/auth/login`
  - Logout: `/api/auth/logout`
  - Access Denied: `/api/auth/access-denied`

### 5. Middleware Configuration
Added in the correct order:
1. CORS
2. **Authentication** (new)
3. **Authorization** (new)

## Next Steps

### 1. Create Migration
Run the following command to create and apply the Identity migration:

```bash
cd src/GameServerHosting.Api
dotnet ef migrations add AddIdentity --output-dir Data/Migrations
dotnet ef database update
```

### 2. Create Authentication Endpoints
Implement authentication controllers/endpoints:
- User Registration (`POST /api/auth/register`)
- User Login (`POST /api/auth/login`)
- User Logout (`POST /api/auth/logout`)
- Password Reset
- Email Confirmation
- Get Current User

### 3. Implement Authorization
- Add role-based authorization attributes to endpoints
- Configure policy-based authorization if needed
- Implement JWT token authentication for API access

### 4. Seed Default Roles
Create a seeding mechanism for default roles:
- Admin
- User
- Support (if needed)

### 5. Add JWT Authentication (Optional)
If you need token-based authentication for API clients:
- Install `Microsoft.AspNetCore.Authentication.JwtBearer`
- Configure JWT settings in `appsettings.json`
- Update Identity configuration to support JWT

## Example Usage

### User Registration (Pseudo-code)
```csharp
var user = new ApplicationUser
{
    UserName = "johndoe",
    Email = "john@example.com",
    FirstName = "John",
    LastName = "Doe",
    Role = UserRole.User,
    IsActive = true,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

var result = await _userManager.CreateAsync(user, "SecurePassword123!");
```

### User Login (Pseudo-code)
```csharp
var user = await _userManager.FindByEmailAsync(email);
var result = await _signInManager.PasswordSignInAsync(
    user.UserName,
    password,
    isPersistent: false,
    lockoutOnFailure: true
);
```

## Security Considerations

1. **Password Hashing**: Identity uses PBKDF2 with HMAC-SHA256 by default
2. **Data Protection**: Tokens are encrypted using ASP.NET Core Data Protection
3. **HTTPS**: Ensure all authentication endpoints use HTTPS
4. **CORS**: Configure CORS appropriately for your client applications
5. **Token Validation**: Implement proper token expiration and refresh logic

## Database Schema Impact

The migration will add the following tables:
- `users` (modified from existing structure)
- `roles`
- `user_roles`
- `user_claims`
- `user_logins`
- `user_tokens`
- `role_claims`

**Note**: The existing `users` table will be modified to include Identity-specific columns like `security_stamp`, `concurrency_stamp`, `email_confirmed`, etc.

## Troubleshooting

### Migration Issues
If the migration conflicts with existing data:
1. Backup your database
2. Review the generated migration file
3. Manually adjust column mappings if needed
4. Consider a data migration script for existing users

### Authentication Not Working
1. Verify middleware order in `Program.cs`
2. Check that `UseAuthentication()` comes before `UseAuthorization()`
3. Ensure cookies are being sent/received correctly
4. Check CORS configuration if calling from a different origin

## References
- [ASP.NET Core Identity Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Identity Configuration Options](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration)
- [Password Options](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration#password)

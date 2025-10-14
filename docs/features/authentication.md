# JWT Authentication Implementation

This document describes the JWT authentication implementation for the Game Server Hosting API.

## Overview

JWT (JSON Web Token) authentication has been fully implemented to secure the API endpoints. The implementation includes:

- ✅ JWT authentication package installation
- ✅ JWT configuration settings
- ✅ Token generation service
- ✅ Token validation middleware
- ✅ Authentication controller with login/register endpoints
- ✅ Authorization support for protected endpoints

## Components

### 1. NuGet Packages

The following packages have been added to `GameServerHosting.Api.csproj`:

- `Microsoft.AspNetCore.Authentication.JwtBearer` (v8.0.11) - JWT Bearer authentication
- `System.IdentityModel.Tokens.Jwt` (v8.2.1) - JWT token handling

### 2. Configuration

JWT settings are configured in `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "REPLACE_WITH_A_STRONG_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG_FOR_PRODUCTION",
    "Issuer": "GameServerHosting.Api",
    "Audience": "GameServerHosting.Client",
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  }
}
```

**Important Security Notes:**
- The `SecretKey` must be at least 32 characters long
- In production, use environment variables for the secret key
- Different secret keys are configured for Development and Production environments

### 3. Models

#### JwtSettings (`Models/JwtSettings.cs`)
Configuration model for JWT settings.

#### AuthResponse (`Models/AuthResponse.cs`)
Response model containing JWT tokens and user information.

#### LoginRequest (`Models/LoginRequest.cs`)
Request model for user login with email and password.

#### RegisterRequest (`Models/RegisterRequest.cs`)
Request model for new user registration with validation.

### 4. Services

#### IJwtTokenService (`Services/IJwtTokenService.cs`)
Interface defining JWT token operations:
- `GenerateAccessToken()` - Creates a JWT access token
- `GenerateRefreshToken()` - Generates a refresh token
- `ValidateToken()` - Validates a JWT token

#### JwtTokenService (`Services/JwtTokenService.cs`)
Implementation of JWT token service with:
- Token generation using HS256 algorithm
- Custom claims (UserId, Email, Role, Name, etc.)
- Token validation with comprehensive error handling
- Support for extended token lifetime (Remember Me)

### 5. Authentication Controller

`Controllers/AuthController.cs` provides the following endpoints:

#### POST /api/auth/register
Registers a new user account.

**Request Body:**
```json
{
  "email": "user@example.com",
  "userName": "username",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "base64string...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "userId": "guid",
  "userName": "username",
  "email": "user@example.com",
  "role": "User"
}
```

#### POST /api/auth/login
Authenticates a user and returns JWT tokens.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "rememberMe": false
}
```

**Response:** Same as register endpoint.

#### GET /api/auth/me
Returns current authenticated user information (requires authentication).

**Headers:**
```
Authorization: Bearer {accessToken}
```

**Response:**
```json
{
  "id": "guid",
  "userName": "username",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "User",
  "isActive": true,
  "createdAt": "2025-10-14T00:00:00Z",
  "lastLoginAt": "2025-10-14T12:00:00Z"
}
```

#### POST /api/auth/logout
Logs out the current user (requires authentication).

### 6. Authentication Middleware

Configured in `Program.cs`:

- JWT Bearer authentication as the default scheme
- Token validation parameters (issuer, audience, signing key)
- Clock skew set to zero for precise expiration
- Support for token from query string (for SignalR)
- Automatic expired token header (`Token-Expired: true`)

## Usage

### Protecting Endpoints

To require authentication on an endpoint, use the `[Authorize]` attribute:

```csharp
[Authorize]
[HttpGet("protected")]
public IActionResult ProtectedEndpoint()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Ok(new { message = "You are authenticated!", userId });
}
```

### Role-Based Authorization

To require a specific role:

```csharp
[Authorize(Roles = "Admin")]
[HttpGet("admin-only")]
public IActionResult AdminOnly()
{
    return Ok(new { message = "Admin access granted" });
}
```

### Accessing User Claims

```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;
var firstName = User.FindFirst("firstName")?.Value;
var lastName = User.FindFirst("lastName")?.Value;
```

## Client Integration

### Making Authenticated Requests

Include the JWT token in the Authorization header:

```javascript
fetch('https://api.example.com/api/auth/me', {
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json'
  }
})
```

### Handling Token Expiration

When a request fails with 401 Unauthorized and the response includes `Token-Expired: true` header, the client should:
1. Use the refresh token to obtain a new access token
2. Retry the failed request with the new token
3. If refresh fails, redirect to login

## Security Considerations

1. **Secret Key Management**
   - Never commit production secret keys to source control
   - Use environment variables or secure key vaults
   - Rotate keys periodically

2. **HTTPS Only**
   - JWT tokens should only be transmitted over HTTPS
   - The `RequireHttpsMetadata` is set to `true` in production

3. **Token Storage**
   - Store tokens securely on the client side
   - Consider using httpOnly cookies for web applications
   - Never store tokens in localStorage in production (XSS risk)

4. **Token Lifetime**
   - Access tokens expire after 60 minutes (configurable)
   - Refresh tokens expire after 7 days (configurable)
   - Adjust based on security requirements

5. **Account Lockout**
   - Failed login attempts trigger account lockout
   - Default: 5 attempts in 15 minutes

## Testing

### Using Swagger UI

1. Navigate to `/swagger` endpoint
2. Click "Authorize" button
3. Enter: `Bearer {your-access-token}`
4. Click "Authorize"
5. All subsequent requests will include the token

### Using curl

```bash
# Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "userName": "testuser",
    "password": "Test123!@#",
    "confirmPassword": "Test123!@#",
    "firstName": "Test",
    "lastName": "User"
  }'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#"
  }'

# Get current user (replace TOKEN with actual token)
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer TOKEN"
```

## Next Steps

1. **Implement Refresh Token Endpoint**
   - Add endpoint to exchange refresh tokens for new access tokens
   - Store refresh tokens in database with expiration

2. **Email Verification**
   - Enable `options.SignIn.RequireConfirmedEmail = true`
   - Implement email sending service
   - Add email confirmation endpoint

3. **Password Reset**
   - Implement forgot password endpoint
   - Create password reset token generation
   - Add reset password endpoint

4. **Two-Factor Authentication (2FA)**
   - Enable 2FA in Identity options
   - Implement TOTP-based 2FA
   - Add 2FA verification endpoints

5. **Audit Logging**
   - Log authentication events
   - Track token generation and validation
   - Monitor failed login attempts

## Troubleshooting

### Token Validation Fails

- Check that the secret key matches between token generation and validation
- Verify the issuer and audience values
- Ensure the token hasn't expired
- Check for clock skew issues

### Unauthorized (401) Responses

- Verify the token is included in the Authorization header
- Check token format: `Bearer {token}`
- Ensure the endpoint has the `[Authorize]` attribute
- Verify the user's role if using role-based authorization

### Registration/Login Fails

- Check password complexity requirements
- Verify email is unique
- Check database connectivity
- Review application logs for detailed errors

## References

- [ASP.NET Core JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [JWT.io - JSON Web Token Introduction](https://jwt.io/introduction)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)

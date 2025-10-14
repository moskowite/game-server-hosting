using GameServerHosting.Api.Models;
using GameServerHosting.Api.Services;
using GameServerHosting.Shared.Entities;
using GameServerHosting.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// Authentication controller for user registration and login
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    // In-memory storage for refresh tokens (in production, use Redis or database)
    private static readonly Dictionary<string, RefreshTokenData> _refreshTokens = new();

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
        _emailService = emailService;
        _logger = logger;
    }

    private class RefreshTokenData
    {
        public Guid UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User with this email already exists" });
            }

            existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
            {
                return BadRequest(new { message = "User with this username already exists" });
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = UserRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, errors);
                return BadRequest(new { message = "Registration failed", errors = result.Errors });
            }

            _logger.LogInformation("New user registered: {UserId} ({Email})", user.Id, user.Email);

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token (expires in 7 days)
            _refreshTokens[refreshToken] = new RefreshTokenData
            {
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = _jwtSettings.ExpirationInMinutes * 60,
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role.ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with tokens</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: User not found for email {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive account: {UserId}", user.Id);
                return Unauthorized(new { message = "Account is not active" });
            }

            // Verify password
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Account locked out: {UserId}", user.Id);
                    return Unauthorized(new { message = "Account is locked. Please try again later." });
                }

                _logger.LogWarning("Login attempt failed: Invalid password for {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User logged in: {UserId} ({Email})", user.Id, user.Email);

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user, request.RememberMe);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token (7 days for regular, 30 days for remember me)
            var refreshTokenExpiry = request.RememberMe ? 30 : 7;
            _refreshTokens[refreshToken] = new RefreshTokenData
            {
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiry)
            };

            var expiresIn = request.RememberMe
                ? _jwtSettings.ExpirationInMinutes * 2 * 60
                : _jwtSettings.ExpirationInMinutes * 60;

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = expiresIn,
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role.ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                Role = user.Role.ToString(),
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Logout the current user
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> Logout()
    {
        try
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out");
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    /// <summary>
    /// Refresh access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication response with tokens</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Validate refresh token
            if (!_refreshTokens.TryGetValue(request.RefreshToken, out var tokenData))
            {
                _logger.LogWarning("Refresh token not found");
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            // Check if token is expired
            if (tokenData.ExpiresAt < DateTime.UtcNow)
            {
                _refreshTokens.Remove(request.RefreshToken);
                _logger.LogWarning("Refresh token expired for user {UserId}", tokenData.UserId);
                return Unauthorized(new { message = "Refresh token has expired" });
            }

            // Get user
            var user = await _userManager.FindByIdAsync(tokenData.UserId.ToString());
            if (user == null || !user.IsActive)
            {
                _refreshTokens.Remove(request.RefreshToken);
                _logger.LogWarning("User not found or inactive for refresh token");
                return Unauthorized(new { message = "Invalid user" });
            }

            // Generate new tokens
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // Remove old refresh token and store new one
            _refreshTokens.Remove(request.RefreshToken);
            _refreshTokens[newRefreshToken] = new RefreshTokenData
            {
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

            var response = new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                TokenType = "Bearer",
                ExpiresIn = _jwtSettings.ExpirationInMinutes * 60,
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role.ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Request a password reset email
    /// </summary>
    /// <param name="request">Forgot password request</param>
    /// <returns>Success message</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            
            // Always return success to prevent email enumeration
            // Don't reveal whether the user exists or not
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Password reset requested for non-existent or inactive user: {Email}", request.Email);
                return Ok(new { message = "If the email exists in our system, a password reset link has been sent." });
            }

            // Generate password reset token
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Send password reset email
            await _emailService.SendPasswordResetEmailAsync(
                user.Email ?? string.Empty,
                resetToken,
                user.UserName ?? string.Empty
            );

            _logger.LogInformation("Password reset email sent to {Email}", request.Email);

            return Ok(new { message = "If the email exists in our system, a password reset link has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred processing your request" });
        }
    }

    /// <summary>
    /// Reset password using a reset token
    /// </summary>
    /// <param name="request">Reset password request</param>
    /// <returns>Success message</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Password reset attempted for non-existent user: {Email}", request.Email);
                return BadRequest(new { message = "Invalid password reset request" });
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Password reset attempted for inactive user: {UserId}", user.Id);
                return BadRequest(new { message = "Account is not active" });
            }

            // Reset password using the token
            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Password reset failed for {Email}: {Errors}", request.Email, errors);
                return BadRequest(new { message = "Password reset failed", errors = result.Errors });
            }

            // Update last updated timestamp
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Password reset successful for user {UserId}", user.Id);

            return Ok(new { message = "Password has been reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during password reset" });
        }
    }
}

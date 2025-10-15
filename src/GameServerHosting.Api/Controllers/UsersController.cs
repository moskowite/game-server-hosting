using GameServerHosting.Application.DTOs;
using GameServerHosting.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameServerHosting.Infrastructure.Data;

namespace GameServerHosting.Api.Controllers;

/// <summary>
/// User profile management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UsersController> _logger;
    private readonly GameServerDbContext _context;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        ILogger<UsersController> logger,
        GameServerDbContext context)
    {
        _userManager = userManager;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    /// <returns>User profile information</returns>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized profile access attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Profile requested for non-existent user: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            var response = new UserProfileResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            };

            _logger.LogInformation("Profile retrieved for user {UserId}", userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, new { message = "An error occurred while retrieving profile" });
        }
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    /// <param name="request">Updated profile information</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized profile update attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Profile update attempted for non-existent user: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            // Check if email is being changed and if it's already taken by another user
            if (user.Email != request.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    _logger.LogWarning("Email already in use: {Email}", request.Email);
                    return BadRequest(new { message = "Email is already in use by another account" });
                }

                // Email is being changed, so set EmailConfirmed to false
                user.Email = request.Email;
                user.NormalizedEmail = request.Email.ToUpperInvariant();
                user.EmailConfirmed = false;
                
                _logger.LogInformation("Email changed for user {UserId}, email confirmation required", userId);
            }

            // Update user properties
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Profile update failed for user {UserId}: {Errors}", userId, errors);
                return BadRequest(new { message = "Profile update failed", errors = result.Errors });
            }

            _logger.LogInformation("Profile updated successfully for user {UserId}", userId);

            var response = new UserProfileResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, new { message = "An error occurred while updating profile" });
        }
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Success message</returns>
    [HttpPut("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized password change attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password change attempted for non-existent user: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            // Verify current password
            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("Invalid current password provided for user {UserId}", userId);
                return BadRequest(new { message = "Current password is incorrect" });
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Password change failed for user {UserId}: {Errors}", userId, errors);
                return BadRequest(new { message = "Password change failed", errors = result.Errors });
            }

            // Update timestamp
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user password");
            return StatusCode(500, new { message = "An error occurred while changing password" });
        }
    }

    /// <summary>
    /// Delete current user's account
    /// </summary>
    /// <returns>Success message</returns>
    [HttpDelete("account")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAccount()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized account deletion attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Account deletion attempted for non-existent user: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            // Instead of hard delete, we'll soft delete by marking as inactive
            // This preserves data integrity and allows for account recovery
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Account deletion failed for user {UserId}: {Errors}", userId, errors);
                return BadRequest(new { message = "Account deletion failed", errors = result.Errors });
            }

            _logger.LogInformation("Account soft deleted for user {UserId}", userId);

            return Ok(new { message = "Account has been deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user account");
            return StatusCode(500, new { message = "An error occurred while deleting account" });
        }
    }

    /// <summary>
    /// Get current user's preferences
    /// </summary>
    /// <returns>User preferences</returns>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserPreferencesDto>> GetPreferences()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized preferences access attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);
            var preferences = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userGuid);

            // If no preferences exist, create default ones
            if (preferences == null)
            {
                preferences = new UserPreferences
                {
                    Id = Guid.NewGuid(),
                    UserId = userGuid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserPreferences.Add(preferences);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created default preferences for user {UserId}", userId);
            }

            var dto = new UserPreferencesDto
            {
                EmailPromotional = preferences.EmailPromotional,
                EmailOrderUpdates = preferences.EmailOrderUpdates,
                EmailServerAlerts = preferences.EmailServerAlerts,
                EmailNewsletter = preferences.EmailNewsletter,
                NotificationsBrowser = preferences.NotificationsBrowser,
                NotificationsServerOffline = preferences.NotificationsServerOffline,
                NotificationsResourceAlerts = preferences.NotificationsResourceAlerts,
                NotificationsPaymentDue = preferences.NotificationsPaymentDue,
                NotificationsSecurityAlerts = preferences.NotificationsSecurityAlerts
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user preferences");
            return StatusCode(500, new { message = "An error occurred while retrieving preferences" });
        }
    }

    /// <summary>
    /// Update current user's preferences
    /// </summary>
    /// <param name="dto">Updated preferences</param>
    /// <returns>Updated preferences</returns>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserPreferencesDto>> UpdatePreferences([FromBody] UserPreferencesDto dto)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized preferences update attempt");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var userGuid = Guid.Parse(userId);
            var preferences = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userGuid);

            // If no preferences exist, create new ones
            if (preferences == null)
            {
                preferences = new UserPreferences
                {
                    Id = Guid.NewGuid(),
                    UserId = userGuid,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserPreferences.Add(preferences);
            }

            // Update preferences
            preferences.EmailPromotional = dto.EmailPromotional;
            preferences.EmailOrderUpdates = dto.EmailOrderUpdates;
            preferences.EmailServerAlerts = dto.EmailServerAlerts;
            preferences.EmailNewsletter = dto.EmailNewsletter;
            preferences.NotificationsBrowser = dto.NotificationsBrowser;
            preferences.NotificationsServerOffline = dto.NotificationsServerOffline;
            preferences.NotificationsResourceAlerts = dto.NotificationsResourceAlerts;
            preferences.NotificationsPaymentDue = dto.NotificationsPaymentDue;
            preferences.NotificationsSecurityAlerts = dto.NotificationsSecurityAlerts;
            preferences.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Preferences updated successfully for user {UserId}", userId);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user preferences");
            return StatusCode(500, new { message = "An error occurred while updating preferences" });
        }
    }
}

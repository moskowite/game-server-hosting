using System.Net.Http.Json;
using System.Text.Json;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Client.Models;
using GameServerHosting.Client.Models.Auth;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace GameServerHosting.Client.Services;

/// <summary>
/// Service for managing authentication state and operations
/// </summary>
public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IJSRuntime _jsRuntime;
    private AuthState _authState = new();
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";
    private const string UserStateKey = "userState";

    public event Action<AuthStateChangedEventArgs>? OnAuthStateChanged;

    public AuthenticationService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<AuthenticationService> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Gets whether the user is currently authenticated
    /// </summary>
    public bool IsAuthenticated => _authState.IsAuthenticated;

    /// <summary>
    /// Gets the current authentication state
    /// </summary>
    public AuthState CurrentAuthState => _authState;

    /// <summary>
    /// Initializes the authentication service by loading stored tokens
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting authentication service initialization");

            var accessToken = await GetFromLocalStorageAsync(AccessTokenKey);
            var refreshToken = await GetFromLocalStorageAsync(RefreshTokenKey);
            var userStateJson = await GetFromLocalStorageAsync(UserStateKey);

            _logger.LogDebug("Retrieved tokens from localStorage: AccessToken={HasValue}, RefreshToken={HasValue}, UserState={HasValue}",
                !string.IsNullOrEmpty(accessToken), !string.IsNullOrEmpty(refreshToken), !string.IsNullOrEmpty(userStateJson));

            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(userStateJson))
            {
                try
                {
                    var userState = JsonSerializer.Deserialize<AuthState>(userStateJson);
                    if (userState != null)
                    {
                        _authState = userState;
                        _authState.AccessToken = accessToken;
                        _authState.RefreshToken = refreshToken;

                        _logger.LogInformation("Loaded authentication state for user: {UserName}", userState.UserName);

                        // Check if token is expired
                        if (_authState.TokenExpiresAt.HasValue && _authState.TokenExpiresAt.Value <= DateTime.UtcNow)
                        {
                            _logger.LogInformation("Access token is expired, attempting to refresh");

                            // Try to refresh token with timeout to prevent hanging
                            try
                            {
                                var refreshTask = RefreshTokenAsync();
                                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));

                                var completedTask = await Task.WhenAny(refreshTask, timeoutTask);
                                if (completedTask == timeoutTask)
                                {
                                    _logger.LogWarning("Token refresh timed out after 5 seconds");
                                    // Refresh timed out, logout to clear invalid state
                                    await LogoutAsync();
                                    return;
                                }

                                var refreshed = await refreshTask;
                                if (refreshed)
                                {
                                    _logger.LogInformation("Token refreshed successfully");
                                }
                                else
                                {
                                    _logger.LogWarning("Token refresh failed");
                                    await LogoutAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error during token refresh");
                                // If refresh fails, logout to clear invalid state
                                await LogoutAsync();
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Access token is still valid");
                            NotifyAuthStateChanged();
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize user state from localStorage");
                        await LogoutAsync();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize authentication state");
                    await LogoutAsync();
                }
            }
            else
            {
                _logger.LogInformation("No valid authentication tokens found, user is not logged in");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during authentication service initialization");
            // If initialization fails, ensure user is logged out
            await LogoutAsync();
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            var request = new LoginRequest
            {
                Email = email,
                Password = password,
                RememberMe = rememberMe
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null)
                {
                    await SetAuthenticationStateAsync(authResponse);
                    return AuthResult.Successful(authResponse, "Login successful");
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = TryParseErrorMessage(errorContent) ?? "Login failed";
            return AuthResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed($"An error occurred during login: {ex.Message}");
        }
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                return AuthResult.Successful(
                    new AuthResponse(), 
                    "Registration successful! Please check your email to verify your account."
                );
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = TryParseErrorMessage(errorContent) ?? "Registration failed";
            return AuthResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed($"An error occurred during registration: {ex.Message}");
        }
    }

    /// <summary>
    /// Logout the current user
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            // Call logout endpoint if authenticated
            if (_authState.IsAuthenticated)
            {
                await _httpClient.PostAsync("/api/auth/logout", null);
            }
        }
        catch
        {
            // Ignore errors during logout API call
        }
        finally
        {
            // Clear local state regardless of API call result
            await ClearAuthenticationStateAsync();
        }
    }

    /// <summary>
    /// Refresh the access token using the refresh token
    /// </summary>
    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = _authState.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Refresh token is null or empty");
                return false;
            }

            var request = new RefreshTokenRequest { RefreshToken = refreshToken };

            // Create request with manual authentication handling
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh-token")
            {
                Content = JsonContent.Create(request)
            };

            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null)
                {
                    await SetAuthenticationStateAsync(authResponse);
                    _logger.LogInformation("Token refreshed successfully");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Auth response was null after successful refresh");
                }
            }
            else
            {
                _logger.LogWarning("Token refresh failed with status code: {StatusCode}", response.StatusCode);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during token refresh");
            return false;
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    public async Task<UserProfileResponse?> GetCurrentUserAsync()
    {
        try
        {
            if (!_authState.IsAuthenticated)
            {
                return null;
            }

            var response = await _httpClient.GetAsync("/api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserProfileResponse>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Verify email address
    /// </summary>
    public async Task<AuthResult> VerifyEmailAsync(string email, string token)
    {
        try
        {
            var request = new VerifyEmailRequest { Email = email, Token = token };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/verify-email", request);

            if (response.IsSuccessStatusCode)
            {
                return AuthResult.Successful(
                    new AuthResponse(),
                    "Email verified successfully! You can now log in."
                );
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = TryParseErrorMessage(errorContent) ?? "Email verification failed";
            return AuthResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed($"An error occurred during email verification: {ex.Message}");
        }
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    public async Task<AuthResult> ForgotPasswordAsync(string email)
    {
        try
        {
            var request = new ForgotPasswordRequest { Email = email };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/forgot-password", request);

            if (response.IsSuccessStatusCode)
            {
                return AuthResult.Successful(
                    new AuthResponse(),
                    "If the email exists in our system, a password reset link has been sent."
                );
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = TryParseErrorMessage(errorContent) ?? "Request failed";
            return AuthResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    public async Task<AuthResult> ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword)
    {
        try
        {
            var request = new ResetPasswordRequest
            {
                Email = email,
                Token = token,
                NewPassword = newPassword,
                ConfirmPassword = confirmPassword
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/reset-password", request);

            if (response.IsSuccessStatusCode)
            {
                return AuthResult.Successful(
                    new AuthResponse(),
                    "Password has been reset successfully! You can now log in."
                );
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = TryParseErrorMessage(errorContent) ?? "Password reset failed";
            return AuthResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed($"An error occurred during password reset: {ex.Message}");
        }
    }

    /// <summary>
    /// Resend email verification
    /// </summary>
    public async Task<AuthResult> ResendVerificationEmailAsync(string email)
    {
        try
        {
            var request = new ResendVerificationEmailRequest { Email = email };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/resend-verification-email", request);

            if (response.IsSuccessStatusCode)
            {
                return AuthResult.Successful(
                    new AuthResponse(),
                    "If the email exists in our system and is not yet verified, a verification link has been sent."
                );
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = TryParseErrorMessage(errorContent) ?? "Request failed";
            return AuthResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the current access token
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        return _authState.AccessToken ?? await GetFromLocalStorageAsync(AccessTokenKey);
    }

    /// <summary>
    /// Set authentication state after successful login/refresh
    /// </summary>
    private async Task SetAuthenticationStateAsync(AuthResponse authResponse)
    {
        _authState = new AuthState
        {
            IsAuthenticated = true,
            UserId = authResponse.UserId,
            UserName = authResponse.UserName,
            Email = authResponse.Email,
            Role = authResponse.Role,
            AccessToken = authResponse.AccessToken,
            RefreshToken = authResponse.RefreshToken,
            TokenExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn)
        };

        // Store tokens in local storage
        await SaveToLocalStorageAsync(AccessTokenKey, authResponse.AccessToken);
        await SaveToLocalStorageAsync(RefreshTokenKey, authResponse.RefreshToken);
        
        // Store user state (without tokens for security)
        var userState = new AuthState
        {
            IsAuthenticated = true,
            UserId = authResponse.UserId,
            UserName = authResponse.UserName,
            Email = authResponse.Email,
            Role = authResponse.Role,
            TokenExpiresAt = _authState.TokenExpiresAt
        };
        await SaveToLocalStorageAsync(UserStateKey, JsonSerializer.Serialize(userState));

        NotifyAuthStateChanged();
    }

    /// <summary>
    /// Clear authentication state
    /// </summary>
    private async Task ClearAuthenticationStateAsync()
    {
        _authState = new AuthState();

        await RemoveFromLocalStorageAsync(AccessTokenKey);
        await RemoveFromLocalStorageAsync(RefreshTokenKey);
        await RemoveFromLocalStorageAsync(UserStateKey);

        NotifyAuthStateChanged();
    }

    /// <summary>
    /// Notify subscribers of authentication state changes
    /// </summary>
    private void NotifyAuthStateChanged()
    {
        OnAuthStateChanged?.Invoke(new AuthStateChangedEventArgs(_authState));
    }

    /// <summary>
    /// Try to parse error message from API response
    /// </summary>
    private string? TryParseErrorMessage(string errorContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(errorContent);
            if (doc.RootElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    // LocalStorage helper methods
    private async Task<string?> GetFromLocalStorageAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveToLocalStorageAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch
        {
            // Ignore storage errors
        }
    }

    private async Task RemoveFromLocalStorageAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch
        {
            // Ignore storage errors
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace GameServerHosting.Client.Services;

/// <summary>
/// HTTP message handler that automatically attaches JWT tokens and handles authentication failures
/// </summary>
public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly AuthenticationService _authService;
    private readonly NavigationManager _navigationManager;
    private bool _refreshing = false;

    public AuthenticatedHttpMessageHandler(
        AuthenticationService authService,
        NavigationManager navigationManager)
    {
        _authService = authService;
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Don't add auth header for auth endpoints
        var uri = request.RequestUri?.ToString() ?? "";
        if (!uri.Contains("/api/auth/login") && 
            !uri.Contains("/api/auth/register") && 
            !uri.Contains("/api/auth/refresh-token") &&
            !uri.Contains("/api/auth/verify-email") &&
            !uri.Contains("/api/auth/forgot-password") &&
            !uri.Contains("/api/auth/reset-password") &&
            !uri.Contains("/api/auth/resend-verification-email"))
        {
            // Attach JWT token to request
            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized
        if (response.StatusCode == HttpStatusCode.Unauthorized && !_refreshing)
        {
            _refreshing = true;
            try
            {
                // Attempt to refresh token
                var refreshed = await _authService.RefreshTokenAsync();
                
                if (refreshed)
                {
                    // Retry the request with new token
                    var newToken = await _authService.GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                        response = await base.SendAsync(request, cancellationToken);
                    }
                }
                else
                {
                    // Refresh failed - redirect to login
                    await _authService.LogoutAsync();
                    _navigationManager.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(_navigationManager.Uri)}");
                }
            }
            finally
            {
                _refreshing = false;
            }
        }

        return response;
    }
}

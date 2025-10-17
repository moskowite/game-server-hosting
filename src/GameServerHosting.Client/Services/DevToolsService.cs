using Microsoft.JSInterop;

namespace GameServerHosting.Client.Services;

/// <summary>
/// Development tools and utilities for better DX
/// Only active in development mode
/// </summary>
public class DevToolsService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<DevToolsService> _logger;

    public DevToolsService(IJSRuntime jsRuntime, ILogger<DevToolsService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Check if running in development mode
    /// </summary>
    public bool IsDevelopment
    {
        get
        {
            #if DEBUG
            return true;
            #else
            return false;
            #endif
        }
    }

    /// <summary>
    /// Log performance metrics for a component
    /// </summary>
    public void LogComponentRender(string componentName, TimeSpan renderTime)
    {
        if (!IsDevelopment) return;

        if (renderTime.TotalMilliseconds > 100)
        {
            _logger.LogWarning(
                "[PERF] Component '{ComponentName}' took {RenderTime}ms to render (slow!)",
                componentName,
                renderTime.TotalMilliseconds
            );
        }
        else
        {
            _logger.LogDebug(
                "[PERF] Component '{ComponentName}' rendered in {RenderTime}ms",
                componentName,
                renderTime.TotalMilliseconds
            );
        }
    }

    /// <summary>
    /// Log state changes for debugging
    /// </summary>
    public void LogStateChange(string componentName, string stateName, object? oldValue, object? newValue)
    {
        if (!IsDevelopment) return;

        _logger.LogDebug(
            "[STATE] {ComponentName}.{StateName}: {OldValue} → {NewValue}",
            componentName,
            stateName,
            oldValue,
            newValue
        );
    }

    /// <summary>
    /// Log API calls for debugging
    /// </summary>
    public void LogApiCall(string method, string endpoint, int statusCode, TimeSpan duration)
    {
        if (!IsDevelopment) return;

        var logLevel = statusCode >= 400 ? LogLevel.Error : LogLevel.Information;

        _logger.Log(
            logLevel,
            "[API] {Method} {Endpoint} → {StatusCode} ({Duration}ms)",
            method,
            endpoint,
            statusCode,
            duration.TotalMilliseconds
        );
    }

    /// <summary>
    /// Enable console logging in browser for debugging
    /// </summary>
    public async Task EnableBrowserLogging()
    {
        if (!IsDevelopment) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("console.log", 
                "%c[DevTools] Browser logging enabled", 
                "color: #10b981; font-weight: bold");
        }
        catch
        {
            // Ignore errors if JS runtime not ready
        }
    }

    /// <summary>
    /// Clear browser console
    /// </summary>
    public async Task ClearConsole()
    {
        if (!IsDevelopment) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("console.clear");
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Get current memory usage (if available)
    /// </summary>
    public async Task<double> GetMemoryUsageMB()
    {
        if (!IsDevelopment) return 0;

        try
        {
            var memoryInfo = await _jsRuntime.InvokeAsync<dynamic>("performance.memory");
            return memoryInfo.usedJSHeapSize / (1024.0 * 1024.0);
        }
        catch
        {
            return 0;
        }
    }
}


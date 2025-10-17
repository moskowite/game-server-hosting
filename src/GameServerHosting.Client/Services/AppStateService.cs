using GameServerHosting.Client.Models;

namespace GameServerHosting.Client.Services;

/// <summary>
/// Centralized application state management with reactive updates
/// Simplifies state sharing across components
/// </summary>
public class AppStateService
{
    // Events for reactive state updates
    public event Action? OnChange;
    public event Action<string>? OnNotification;
    public event Action<string, string>? OnError;

    // Global loading state
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                NotifyStateChanged();
            }
        }
    }

    // Global error state
    private string? _globalError;
    public string? GlobalError
    {
        get => _globalError;
        set
        {
            if (_globalError != value)
            {
                _globalError = value;
                if (!string.IsNullOrEmpty(value))
                {
                    OnError?.Invoke("Error", value);
                }
                NotifyStateChanged();
            }
        }
    }

    // Page title
    private string _pageTitle = "Dashboard";
    public string PageTitle
    {
        get => _pageTitle;
        set
        {
            if (_pageTitle != value)
            {
                _pageTitle = value;
                NotifyStateChanged();
            }
        }
    }

    // Mobile menu state
    private bool _isMobileMenuOpen;
    public bool IsMobileMenuOpen
    {
        get => _isMobileMenuOpen;
        set
        {
            if (_isMobileMenuOpen != value)
            {
                _isMobileMenuOpen = value;
                NotifyStateChanged();
            }
        }
    }

    /// <summary>
    /// Executes an async operation with automatic loading state management
    /// </summary>
    public async Task<T?> ExecuteWithLoadingAsync<T>(Func<Task<T>> operation, string? loadingMessage = null)
    {
        try
        {
            IsLoading = true;
            if (!string.IsNullOrEmpty(loadingMessage))
            {
                ShowNotification(loadingMessage);
            }

            return await operation();
        }
        catch (Exception ex)
        {
            GlobalError = ex.Message;
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Executes an async operation with automatic loading and error handling
    /// </summary>
    public async Task ExecuteWithLoadingAsync(Func<Task> operation, string? loadingMessage = null, string? successMessage = null)
    {
        try
        {
            IsLoading = true;
            if (!string.IsNullOrEmpty(loadingMessage))
            {
                ShowNotification(loadingMessage);
            }

            await operation();

            if (!string.IsNullOrEmpty(successMessage))
            {
                ShowNotification(successMessage);
            }
        }
        catch (Exception ex)
        {
            GlobalError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Show a temporary notification
    /// </summary>
    public void ShowNotification(string message)
    {
        OnNotification?.Invoke(message);
    }

    /// <summary>
    /// Show an error message
    /// </summary>
    public void ShowError(string title, string message)
    {
        OnError?.Invoke(title, message);
    }

    /// <summary>
    /// Clear global error
    /// </summary>
    public void ClearError()
    {
        GlobalError = null;
    }

    /// <summary>
    /// Toggle mobile menu
    /// </summary>
    public void ToggleMobileMenu()
    {
        IsMobileMenuOpen = !IsMobileMenuOpen;
    }

    /// <summary>
    /// Notify subscribers that state has changed
    /// </summary>
    private void NotifyStateChanged() => OnChange?.Invoke();
}


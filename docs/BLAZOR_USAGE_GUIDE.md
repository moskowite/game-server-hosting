# Blazor Components Usage Guide

Quick reference for using the new Blazor components and services.

---

## 🎯 AppStateService

### Basic Usage

```csharp
@inject AppStateService AppState

// Execute async operation with automatic loading state
private async Task LoadDataAsync()
{
    await AppState.ExecuteWithLoadingAsync(
        async () => {
            var data = await ApiService.GetDataAsync();
            items = data;
        },
        "Loading data...",      // Loading message (optional)
        "Data loaded!"          // Success message (optional)
    );
}

// Show notification
AppState.ShowNotification("Server deployed successfully!");

// Show error
AppState.ShowError("Connection Error", "Failed to connect to API");

// Clear error
AppState.ClearError();

// Set page title
AppState.PageTitle = "Server Management";

// Toggle mobile menu
AppState.ToggleMobileMenu();
```

### Subscribe to State Changes

```csharp
@inject AppStateService AppState
@implements IDisposable

protected override void OnInitialized()
{
    AppState.OnChange += StateHasChanged;
    AppState.OnNotification += HandleNotification;
    AppState.OnError += HandleError;
}

private void HandleNotification(string message)
{
    // Handle notification
}

private void HandleError(string title, string message)
{
    // Handle error
}

public void Dispose()
{
    AppState.OnChange -= StateHasChanged;
    AppState.OnNotification -= HandleNotification;
    AppState.OnError -= HandleError;
}
```

---

## 🎨 LoadingSpinner

### Basic Usage

```razor
@if (isLoading)
{
    <LoadingSpinner Message="Loading servers..." />
}
```

### Different Sizes

```razor
<!-- Small -->
<LoadingSpinner Size="LoadingSpinner.LoadingSize.Small" />

<!-- Medium (default) -->
<LoadingSpinner Size="LoadingSpinner.LoadingSize.Medium" />

<!-- Large -->
<LoadingSpinner Size="LoadingSpinner.LoadingSize.Large" />
```

### Without Message

```razor
<LoadingSpinner />
```

---

## 🔄 LoadingOverlay

### Basic Usage

```razor
<LoadingOverlay IsLoading="@isProcessing" LoadingMessage="Processing payment..." />
```

### With AppStateService (Global)

```csharp
// Any component can set global loading
AppState.IsLoading = true;

// The LoadingOverlay in MainLayout will automatically show
```

---

## 🚨 ErrorAlert

### Basic Usage

```razor
<ErrorAlert ErrorMessage="@errorMessage" />
```

### With All Options

```razor
<ErrorAlert 
    ErrorMessage="@errorMessage"
    Title="Operation Failed"
    Details="@technicalDetails"
    Severity="AlertSeverity.Error"
    Dismissible="true"
    ShowDetails="true"
    OnDismiss="@HandleDismiss" />
```

### Different Severities

```razor
<!-- Error (red) -->
<ErrorAlert ErrorMessage="Error occurred" Severity="AlertSeverity.Error" />

<!-- Warning (yellow) -->
<ErrorAlert ErrorMessage="Warning message" Severity="AlertSeverity.Warning" />

<!-- Info (blue) -->
<ErrorAlert ErrorMessage="Info message" Severity="AlertSeverity.Info" />

<!-- Success (green) -->
<ErrorAlert ErrorMessage="Success!" Severity="AlertSeverity.Success" />
```

---

## 🛡️ AppErrorBoundary

### Wrapping a Component

```razor
<AppErrorBoundary IsDevelopment="@isDevelopment">
    <ChildContent>
        <MyComponent />
    </ChildContent>
</AppErrorBoundary>
```

### Wrapping a Section

```razor
<AppErrorBoundary>
    <ChildContent>
        @if (data != null)
        {
            foreach (var item in data)
            {
                <DataCard Item="@item" />
            }
        }
    </ChildContent>
</AppErrorBoundary>
```

### Manual Recovery

```razor
<AppErrorBoundary @ref="AppErrorBoundary">
    <ChildContent>
        <!-- Your content -->
    </ChildContent>
</AppErrorBoundary>

@code {
    private AppErrorBoundary? AppErrorBoundary;
    
    private void RetryOperation()
    {
        AppErrorBoundary?.Recover();
        // Try operation again
    }
}
```

---

## 🔔 ToastNotification

### Usage via AppStateService

```csharp
// Info toast (auto-dismisses after 5 seconds)
AppState.ShowNotification("Server started successfully");

// Error toast (stays until dismissed)
AppState.ShowError("Error", "Failed to connect to server");
```

### No Direct Component Usage
The `<ToastNotification />` component is added to `MainLayout.razor` once and listens to AppStateService events.

---

## 🛠️ DevPanel

### Automatic Usage
- Added to `MainLayout.razor`
- Only visible in development mode (`#if DEBUG`)
- Click the floating code icon to open
- Shows real-time app state, memory usage, and quick actions

### No Manual Configuration Needed
The DevPanel automatically subscribes to AppStateService and updates in real-time.

---

## 💡 Common Patterns

### Loading Data on Page Load

```csharp
@page "/servers"
@inject AppStateService AppState
@inject ServerService ServerService

<PageTitle>Servers</PageTitle>

<ErrorAlert ErrorMessage="@AppState.GlobalError" />

@if (servers == null)
{
    <LoadingSpinner Message="Loading servers..." />
}
else
{
    <ServerList Servers="@servers" />
}

@code {
    private List<Server>? servers;
    
    protected override async Task OnInitializedAsync()
    {
        AppState.PageTitle = "Servers";
        
        await AppState.ExecuteWithLoadingAsync(async () => {
            servers = await ServerService.GetServersAsync();
        });
    }
}
```

### Form Submission with Feedback

```csharp
private async Task HandleSubmitAsync()
{
    await AppState.ExecuteWithLoadingAsync(
        async () => {
            await ServerService.CreateServerAsync(model);
            NavManager.NavigateTo("/servers");
        },
        "Creating server...",
        "Server created successfully!"
    );
}
```

### Error Handling in Try-Catch

```csharp
private string? errorMessage;

private async Task DeleteServerAsync(Guid id)
{
    try
    {
        AppState.IsLoading = true;
        await ServerService.DeleteAsync(id);
        AppState.ShowNotification("Server deleted");
        await LoadServersAsync();
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
        AppState.ShowError("Delete Failed", ex.Message);
    }
    finally
    {
        AppState.IsLoading = false;
    }
}
```

### Conditional Error Display

```razor
@if (!string.IsNullOrEmpty(errorMessage))
{
    <ErrorAlert 
        ErrorMessage="@errorMessage"
        Dismissible="true"
        OnDismiss="@(() => errorMessage = null)" />
}
```

---

## 📋 Complete Example Component

```razor
@page "/server/{Id:guid}"
@inject AppStateService AppState
@inject ServerService ServerService
@inject NavigationManager NavManager

<PageTitle>@(server?.Name ?? "Loading...")</PageTitle>

<AppErrorBoundary>
    <ChildContent>
        <div class="server-details">
            <ErrorAlert ErrorMessage="@errorMessage" Dismissible="true" />
            
            @if (server == null)
            {
                <LoadingSpinner Message="Loading server details..." Size="LoadingSpinner.LoadingSize.Large" />
            }
            else
            {
                <h1>@server.Name</h1>
                <p>@server.Status</p>
                
                <button class="btn btn-primary" @onclick="StartServerAsync" disabled="@(AppState.IsLoading)">
                    @if (AppState.IsLoading)
                    {
                        <LoadingSpinner Size="LoadingSpinner.LoadingSize.Small" />
                    }
                    else
                    {
                        <span>Start Server</span>
                    }
                </button>
            }
        </div>
    </ChildContent>
</AppErrorBoundary>

<LoadingOverlay IsLoading="@isProcessing" LoadingMessage="@processingMessage" />

@code {
    [Parameter]
    public Guid Id { get; set; }
    
    private Server? server;
    private string? errorMessage;
    private bool isProcessing;
    private string? processingMessage;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadServerAsync();
    }
    
    private async Task LoadServerAsync()
    {
        await AppState.ExecuteWithLoadingAsync(async () => {
            server = await ServerService.GetByIdAsync(Id);
            AppState.PageTitle = server?.Name ?? "Server";
        });
    }
    
    private async Task StartServerAsync()
    {
        isProcessing = true;
        processingMessage = "Starting server...";
        
        try
        {
            await ServerService.StartAsync(Id);
            AppState.ShowNotification("Server started successfully!");
            await LoadServerAsync();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            AppState.ShowError("Start Failed", ex.Message);
        }
        finally
        {
            isProcessing = false;
        }
    }
}
```

---

## 🎨 Styling Tips

### Custom Loading Messages
Make loading messages user-friendly and specific:
- ✅ "Loading your servers..."
- ✅ "Deploying Minecraft server..."
- ❌ "Loading..."
- ❌ "Please wait..."

### Error Messages
Make errors actionable:
- ✅ "Connection failed. Please check your internet connection."
- ✅ "Server not found. It may have been deleted."
- ❌ "Error occurred"
- ❌ "500 Internal Server Error"

### Toast Notifications
Keep them short and sweet:
- ✅ "Server deployed!"
- ✅ "Settings saved"
- ❌ "The server has been successfully deployed to the cloud"

---

## 🚀 Performance Tips

1. **Use `LoadingSpinner.Small` for inline loading** (like buttons)
2. **Use `LoadingOverlay` for page-wide operations** (like saving)
3. **Minimize AppState.OnChange subscriptions** - only subscribe when needed
4. **Use AppErrorBoundary strategically** - wrap components that might fail, not everything
5. **Clear errors when navigating** - use `OnDismiss` or clear on navigation

---

## ✅ Migration Checklist

When updating an existing component:

- [ ] Replace custom loading spinners with `<LoadingSpinner />`
- [ ] Replace manual loading state with `AppStateService.ExecuteWithLoadingAsync()`
- [ ] Replace custom error displays with `<ErrorAlert />`
- [ ] Add `<AppErrorBoundary>` around risky operations
- [ ] Use `AppState.ShowNotification()` for success feedback
- [ ] Use `AppState.ShowError()` for error feedback
- [ ] Remove manual `StateHasChanged()` calls (handled by AppStateService)
- [ ] Set `AppState.PageTitle` in `OnInitializedAsync()`

---

## 📚 Additional Resources

- **Full Documentation**: See `BLAZOR_IMPROVEMENTS_SUMMARY.md`
- **DevPanel**: Press the floating code icon in development mode
- **Error Logs**: Check browser console for detailed error information
- **Performance**: Use DevPanel to monitor component render times

---

Happy coding! 🎉


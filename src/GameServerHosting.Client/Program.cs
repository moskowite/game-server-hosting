using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Client;
using GameServerHosting.Client.Models;
using GameServerHosting.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API settings from appsettings.json (environment-specific)
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() 
    ?? new ApiSettings { BaseUrl = builder.HostEnvironment.BaseAddress };

// Configure Stripe settings
var stripeSettings = builder.Configuration.GetSection("Stripe").Get<StripeSettings>() 
    ?? new StripeSettings();
builder.Services.AddSingleton(stripeSettings);

// Register AuthenticationService as scoped since it depends on HttpClient
// Note: IJSRuntime is also scoped in Blazor WASM
builder.Services.AddScoped<AuthenticationService>();

// Register AppStateService as singleton for global state management
builder.Services.AddSingleton<AppStateService>();

// Register DevToolsService for better development experience
builder.Services.AddSingleton<DevToolsService>();

// Configure HttpClient for API communication
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri(apiSettings.BaseUrl)
    };

    return httpClient;
});

// Register application services
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ServerCatalogService>();
builder.Services.AddScoped<ServerPlansService>();
builder.Services.AddScoped<ServerManagementService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<PaymentService>();

await builder.Build().RunAsync();

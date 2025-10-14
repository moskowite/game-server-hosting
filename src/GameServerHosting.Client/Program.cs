using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GameServerHosting.Client;
using GameServerHosting.Client.Models;
using GameServerHosting.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API settings from appsettings.json (environment-specific)
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>() 
    ?? new ApiSettings { BaseUrl = builder.HostEnvironment.BaseAddress };

builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiSettings.BaseUrl) 
});

// Register services
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ServerCatalogService>();
builder.Services.AddScoped<ServerPlansService>();
builder.Services.AddScoped<CartService>();

await builder.Build().RunAsync();

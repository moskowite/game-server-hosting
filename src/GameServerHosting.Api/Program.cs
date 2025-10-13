using GameServerHosting.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Game Server Hosting API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add services to the container.
    // Configure PostgreSQL with Entity Framework Core
    builder.Services.AddDbContext<GameServerDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Configure CORS
    var corsConfig = builder.Configuration.GetSection("Cors");
    var policyName = corsConfig.GetValue<string>("PolicyName") ?? "GameServerHostingPolicy";
    var allowedOrigins = corsConfig.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    var allowedMethods = corsConfig.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE" };
    var allowedHeaders = corsConfig.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "*" };
    var allowCredentials = corsConfig.GetValue<bool>("AllowCredentials");
    var maxAge = corsConfig.GetValue<int?>("MaxAge");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(policyName, policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins);
            }
            else
            {
                policy.AllowAnyOrigin();
            }

            if (allowedMethods.Length > 0)
            {
                policy.WithMethods(allowedMethods);
            }
            else
            {
                policy.AllowAnyMethod();
            }

            if (allowedHeaders.Length > 0 && allowedHeaders[0] == "*")
            {
                policy.AllowAnyHeader();
            }
            else if (allowedHeaders.Length > 0)
            {
                policy.WithHeaders(allowedHeaders);
            }

            if (allowCredentials)
            {
                policy.AllowCredentials();
            }

            if (maxAge.HasValue)
            {
                policy.SetPreflightMaxAge(TimeSpan.FromSeconds(maxAge.Value));
            }
        });
    });

    // Configure Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    
    var swaggerConfig = builder.Configuration.GetSection("Swagger");
    var swaggerEnabled = swaggerConfig.GetValue<bool>("Enabled", true);
    
    if (swaggerEnabled)
    {
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = swaggerConfig.GetValue<string>("Title") ?? "Game Server Hosting API",
                Version = swaggerConfig.GetValue<string>("Version") ?? "v1",
                Description = swaggerConfig.GetValue<string>("Description") ?? "API for managing game server hosting services",
                Contact = new OpenApiContact
                {
                    Name = swaggerConfig.GetValue<string>("Contact:Name") ?? "Support Team",
                    Email = swaggerConfig.GetValue<string>("Contact:Email"),
                    Url = swaggerConfig.GetValue<string>("Contact:Url") != null 
                        ? new Uri(swaggerConfig.GetValue<string>("Contact:Url")!) 
                        : null
                }
            });

            // Add security definition for Bearer token (for future authentication)
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseSerilogRequestLogging();

    // Enable CORS
    app.UseCors(policyName);

    // Enable Swagger based on configuration
    if (swaggerEnabled)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Game Server Hosting API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Game Server Hosting API Documentation";
        });
    }

    app.UseHttpsRedirection();

    // Sample endpoint (can be removed later)
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        
        Log.Information("Weather forecast requested, returning {Count} items", forecast.Length);
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi()
    .WithTags("Sample");

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithName("HealthCheck")
        .WithOpenApi()
        .WithTags("Health");

    Log.Information("Application configured successfully. Environment: {Environment}", app.Environment.EnvironmentName);
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}2 + (int)(TemperatureC / 0.5556);
}

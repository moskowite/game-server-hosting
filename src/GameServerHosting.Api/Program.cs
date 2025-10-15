using System.Text;
using GameServerHosting.Api.Data;
using GameServerHosting.Api.Jobs;
using GameServerHosting.Api.Models;
using GameServerHosting.Api.Services;
using GameServerHosting.Shared.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
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

    // Configure JWT Settings
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    builder.Services.Configure<JwtSettings>(jwtSettings);
    
    // Configure Email Settings
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
    
    // Configure Stripe Settings
    builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
    
    // Configure Provisioning Settings
    builder.Services.Configure<ProvisioningSettings>(builder.Configuration.GetSection("Provisioning"));
    builder.Services.Configure<CloudProvidersSettings>(builder.Configuration.GetSection("CloudProviders"));
    builder.Services.Configure<DigitalOceanSettings>(builder.Configuration.GetSection("CloudProviders:DigitalOcean"));
    
    var jwtSecretKey = jwtSettings.GetValue<string>("SecretKey") ?? throw new InvalidOperationException("JWT SecretKey is not configured");
    var jwtIssuer = jwtSettings.GetValue<string>("Issuer") ?? "GameServerHosting.Api";
    var jwtAudience = jwtSettings.GetValue<string>("Audience") ?? "GameServerHosting.Client";

    // Configure ASP.NET Core Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 4;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        // Sign-in settings
        options.SignIn.RequireConfirmedEmail = true; // Email verification is now required
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<GameServerDbContext>()
    .AddDefaultTokenProviders();

    // Configure JWT Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                // Support token from query string for SignalR connections
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    // Register JWT Token Service
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

    // Register Email Service
    builder.Services.AddScoped<IEmailService, EmailService>();

    // Register Stripe Service
    builder.Services.AddScoped<IStripeService, StripeService>();

    // Register Order Validation Service
    builder.Services.AddScoped<IOrderValidationService, OrderValidationService>();

    // Register Payment Service
    builder.Services.AddScoped<IPaymentService, PaymentService>();

    // Register Invoice Service
    builder.Services.AddScoped<IInvoiceService, InvoiceService>();

    // Register Server Provisioning Services
    builder.Services.AddHttpClient<ICloudProviderService, DigitalOceanProviderService>();
    builder.Services.AddScoped<IServerProvisioningService, ServerProvisioningService>();
    
    // Register Server Management Services
    builder.Services.AddScoped<IServerManagementService, ServerManagementService>();
    builder.Services.AddScoped<IServerStatusNotificationService, ServerStatusNotificationService>();

    // Register Background Job Services
    builder.Services.AddScoped<ServerProvisioningJob>();
    builder.Services.AddScoped<ServerHealthCheckJob>();
    builder.Services.AddScoped<IJobSchedulerService, JobSchedulerService>();

    // Configure Quartz.NET for background job processing
    builder.Services.AddQuartz(q =>
    {
        // Use a unique scheduler instance name
        q.SchedulerName = "GameServerHostingScheduler";
        q.SchedulerId = "AUTO";

        // Configure job store and serialization
        q.UseSimpleTypeLoader();
        q.UseInMemoryStore();
        q.UseDefaultThreadPool(tp =>
        {
            tp.MaxConcurrency = 10;
        });

        // Configure retry logic for provisioning jobs
        q.AddJob<QuartzServerProvisioningJob>(opts => opts
            .WithIdentity("default-provisioning-job")
            .StoreDurably()
        );

        // Configure the health check job with a schedule
        var healthCheckConfig = builder.Configuration.GetSection("Jobs:HealthCheck");
        var healthCheckEnabled = healthCheckConfig.GetValue<bool>("Enabled", true);
        var healthCheckSchedule = healthCheckConfig.GetValue<string>("CronSchedule") ?? "0 0/5 * * * ?";

        if (healthCheckEnabled)
        {
            var healthCheckJobKey = new JobKey("health-check-job", "monitoring");
            q.AddJob<QuartzServerHealthCheckJob>(opts => opts
                .WithIdentity(healthCheckJobKey)
                .WithDescription("Periodic health check for all running servers")
            );

            q.AddTrigger(opts => opts
                .ForJob(healthCheckJobKey)
                .WithIdentity("health-check-trigger", "monitoring")
                .WithCronSchedule(healthCheckSchedule)
                .WithDescription($"Trigger for health check job: {healthCheckSchedule}")
            );
        }
    });

    // Add Quartz hosted service
    builder.Services.AddQuartzHostedService(options =>
    {
        options.WaitForJobsToComplete = true;
        options.AwaitApplicationStarted = true;
    });

    // Configure CORS
    var corsConfig = builder.Configuration.GetSection("Cors");
    var policyName = corsConfig.GetValue<string>("PolicyName") ?? "GameServerHostingPolicy";
    var allowAnyLocalhostOrigin = corsConfig.GetValue<bool>("AllowAnyLocalhostOrigin");
    var allowedOrigins = corsConfig.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    var allowedMethods = corsConfig.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE" };
    var allowedHeaders = corsConfig.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "*" };
    var allowCredentials = corsConfig.GetValue<bool>("AllowCredentials");
    var maxAge = corsConfig.GetValue<int?>("MaxAge");

    // Production safety validation - prevent AllowAnyLocalhostOrigin in non-Development environments
    if (!builder.Environment.IsDevelopment() && allowAnyLocalhostOrigin)
    {
        throw new InvalidOperationException(
            "AllowAnyLocalhostOrigin cannot be enabled in Production or Staging environments. " +
            "This setting is only allowed when ASPNETCORE_ENVIRONMENT=Development.");
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(policyName, policy =>
        {
            // In Development, allow any localhost origin if configured
            if (builder.Environment.IsDevelopment() && allowAnyLocalhostOrigin)
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrWhiteSpace(origin))
                        return false;

                    var uri = new Uri(origin);
                    // Allow any localhost or 127.0.0.1 origin on any port
                    return uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host == "[::1]";
                });
                
                Log.Information("CORS: Allowing all localhost origins for Development environment");
            }
            else if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins);
                Log.Information("CORS: Allowing origins: {Origins}", string.Join(", ", allowedOrigins));
            }
            else
            {
                policy.AllowAnyOrigin();
                Log.Warning("CORS: Allowing ANY origin - this should only be used in development");
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

    // Add Controllers
    builder.Services.AddControllers();

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

    // Seed the database with initial data
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<GameServerDbContext>();
            var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();
            var seeder = new DatabaseSeeder(context, logger);
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }

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

    // Enable Authentication and Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Map Controllers
    app.MapControllers();

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
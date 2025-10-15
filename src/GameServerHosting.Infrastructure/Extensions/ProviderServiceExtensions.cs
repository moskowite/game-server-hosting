using GameServerHosting.Application.DTOs;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Infrastructure.Providers.Cloud;
using GameServerHosting.Infrastructure.Providers.Payment;
using GameServerHosting.Infrastructure.Providers.Strategies;
using GameServerHosting.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace GameServerHosting.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering provider services with DI and resilience policies
/// </summary>
public static class ProviderServiceExtensions
{
    /// <summary>
    /// Registers all cloud and payment provider services with typed HttpClients and Polly policies
    /// </summary>
    public static IServiceCollection AddProviderServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure provider settings
        services.Configure<DigitalOceanSettings>(configuration.GetSection("CloudProviders:DigitalOcean"));
        services.Configure<AwsEc2Settings>(configuration.GetSection("CloudProviders:AWS"));
        services.Configure<AzureVmSettings>(configuration.GetSection("CloudProviders:Azure"));
        services.Configure<PayPalSettings>(configuration.GetSection("PaymentGateways:PayPal"));
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
        
        // Provider selection strategies
        services.Configure<CloudProviderSelectionSettings>(configuration.GetSection("CloudProviders"));
        services.Configure<PaymentGatewaySettings>(configuration.GetSection("PaymentGateways"));

        // Register strategy resolvers
        services.AddSingleton<ICloudProviderStrategy, CloudProviderStrategy>();
        services.AddSingleton<IPaymentGatewayStrategy, PaymentGatewayStrategy>();

        // Register cloud providers with typed HttpClients and resilience
        RegisterCloudProviders(services);

        // Register payment gateways with typed HttpClients and resilience
        RegisterPaymentGateways(services);

        return services;
    }

    private static void RegisterCloudProviders(IServiceCollection services)
    {
        // DigitalOcean Cloud Provider
        services.AddHttpClient<DigitalOceanProviderService>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy(TimeSpan.FromSeconds(30)));

        // AWS EC2 Cloud Provider
        services.AddHttpClient<AwsEc2CloudProvider>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy(TimeSpan.FromSeconds(30)));

        // Azure VM Cloud Provider
        services.AddHttpClient<AzureVmCloudProvider>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy(TimeSpan.FromSeconds(30)));

        // Register all implementations for strategy resolution
        services.AddScoped<ICloudProviderService>(sp =>
        {
            var strategy = sp.GetRequiredService<ICloudProviderStrategy>();
            // Default context - can be overridden when resolving
            return strategy.GetProvider(new ProviderSelectionContext());
        });
    }

    private static void RegisterPaymentGateways(IServiceCollection services)
    {
        // Stripe Payment Gateway
        services.AddHttpClient<StripePaymentGateway>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy(TimeSpan.FromSeconds(30)));

        // PayPal Payment Gateway
        services.AddHttpClient<PayPalPaymentGateway>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy())
            .AddPolicyHandler(GetTimeoutPolicy(TimeSpan.FromSeconds(30)));

        // Register default gateway for DI
        services.AddScoped<IPaymentGateway>(sp =>
        {
            var strategy = sp.GetRequiredService<IPaymentGatewayStrategy>();
            // Default context - can be overridden when resolving
            return strategy.GetGateway(new PaymentSelectionContext());
        });
    }

    #region Polly Resilience Policies

    /// <summary>
    /// Exponential backoff retry policy for transient failures
    /// Retries 3 times with delays of 2s, 4s, 8s
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Retry attempt {RetryAttempt} after {Delay}ms due to: {Result}",
                        retryAttempt,
                        timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    /// <summary>
    /// Circuit breaker policy to prevent cascading failures
    /// Opens circuit after 5 consecutive failures, stays open for 30 seconds
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Circuit breaker opened for {Duration}s due to: {Result}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: context =>
                {
                    var logger = context.GetLogger();
                    logger?.LogInformation("Circuit breaker reset");
                });
    }

    /// <summary>
    /// Timeout policy to prevent hanging requests
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout,
            onTimeoutAsync: (context, timespan, task) =>
            {
                var logger = context.GetLogger();
                logger?.LogWarning(
                    "Request timed out after {Timeout}s",
                    timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Extension method to get logger from Polly context
    /// </summary>
    private static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue("ILogger", out var logger))
        {
            return logger as ILogger;
        }
        return null;
    }

    #endregion
}

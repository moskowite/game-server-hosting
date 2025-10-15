using GameServerHosting.Application.Interfaces;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Infrastructure.Providers.Cloud;
using GameServerHosting.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameServerHosting.Infrastructure.Providers.Strategies;

/// <summary>
/// Strategy pattern implementation for selecting cloud providers at runtime
/// </summary>
public interface ICloudProviderStrategy
{
    /// <summary>
    /// Resolves the appropriate cloud provider based on context
    /// </summary>
    /// <param name="context">Provider selection context</param>
    /// <returns>The selected cloud provider instance</returns>
    ICloudProviderService GetProvider(ProviderSelectionContext context);

    /// <summary>
    /// Gets a specific cloud provider by name
    /// </summary>
    /// <param name="providerName">Provider name (DigitalOcean, AWS, Azure)</param>
    /// <returns>The cloud provider instance</returns>
    ICloudProviderService GetProviderByName(string providerName);
}

/// <summary>
/// Implementation of cloud provider strategy resolver
/// </summary>
public class CloudProviderStrategy : ICloudProviderStrategy
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CloudProviderSelectionSettings _settings;
    private readonly ILogger<CloudProviderStrategy> _logger;
    private readonly Dictionary<string, Type> _providerTypes;

    public CloudProviderStrategy(
        IServiceProvider serviceProvider,
        IOptions<CloudProviderSelectionSettings> settings,
        ILogger<CloudProviderStrategy> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;

        // Map provider names to their implementation types
        _providerTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["DigitalOcean"] = typeof(Services.DigitalOceanProviderService),
            ["AWS"] = typeof(Cloud.AwsEc2CloudProvider),
            ["Azure"] = typeof(Cloud.AzureVmCloudProvider)
        };
    }

    /// <inheritdoc/>
    public ICloudProviderService GetProvider(ProviderSelectionContext context)
    {
        string? selectedProvider = null;

        // Strategy 1: Check plan-based provider mapping
        if (!string.IsNullOrEmpty(context.PlanName) && 
            _settings.PlanProviders.TryGetValue(context.PlanName, out var planProvider))
        {
            selectedProvider = planProvider;
            _logger.LogInformation(
                "Selected cloud provider {Provider} based on plan {Plan}",
                selectedProvider, context.PlanName);
        }
        // Strategy 2: Check game type-based provider mapping
        else if (!string.IsNullOrEmpty(context.GameType) && 
                 _settings.GameTypeProviders.TryGetValue(context.GameType, out var gameProvider))
        {
            selectedProvider = gameProvider;
            _logger.LogInformation(
                "Selected cloud provider {Provider} based on game type {GameType}",
                selectedProvider, context.GameType);
        }
        // Strategy 3: Check region-based provider mapping
        else if (!string.IsNullOrEmpty(context.Region) && 
                 _settings.RegionProviders.TryGetValue(context.Region, out var regionProvider))
        {
            selectedProvider = regionProvider;
            _logger.LogInformation(
                "Selected cloud provider {Provider} based on region {Region}",
                selectedProvider, context.Region);
        }
        // Strategy 4: Use default provider
        else
        {
            selectedProvider = _settings.DefaultProvider;
            _logger.LogInformation(
                "Selected default cloud provider {Provider}",
                selectedProvider);
        }

        return GetProviderByName(selectedProvider);
    }

    /// <inheritdoc/>
    public ICloudProviderService GetProviderByName(string providerName)
    {
        if (!_providerTypes.TryGetValue(providerName, out var providerType))
        {
            _logger.LogWarning(
                "Unknown cloud provider {Provider}, falling back to {Default}",
                providerName, _settings.DefaultProvider);

            providerName = _settings.DefaultProvider;
            providerType = _providerTypes[providerName];
        }

        var provider = _serviceProvider.GetService(providerType) as ICloudProviderService;
        
        if (provider == null)
        {
            throw new InvalidOperationException(
                $"Cloud provider {providerName} is not registered in DI container");
        }

        return provider;
    }
}

/// <summary>
/// Context information for provider selection
/// </summary>
public class ProviderSelectionContext
{
    /// <summary>
    /// Game type (Minecraft, Valheim, etc.)
    /// </summary>
    public string? GameType { get; set; }

    /// <summary>
    /// Server plan name
    /// </summary>
    public string? PlanName { get; set; }

    /// <summary>
    /// Preferred region
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// User ID for potential user-based routing
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Additional custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

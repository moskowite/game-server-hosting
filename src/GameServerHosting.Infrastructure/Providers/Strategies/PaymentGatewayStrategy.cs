using GameServerHosting.Application.Interfaces;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Infrastructure.Providers.Payment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameServerHosting.Infrastructure.Providers.Strategies;

/// <summary>
/// Strategy pattern implementation for selecting payment gateways at runtime
/// </summary>
public interface IPaymentGatewayStrategy
{
    /// <summary>
    /// Resolves the appropriate payment gateway based on context
    /// </summary>
    /// <param name="context">Payment selection context</param>
    /// <returns>The selected payment gateway instance</returns>
    IPaymentGateway GetGateway(PaymentSelectionContext context);

    /// <summary>
    /// Gets a specific payment gateway by name
    /// </summary>
    /// <param name="gatewayName">Gateway name (Stripe, PayPal)</param>
    /// <returns>The payment gateway instance</returns>
    IPaymentGateway GetGatewayByName(string gatewayName);

    /// <summary>
    /// Gets all available payment gateway names
    /// </summary>
    IEnumerable<string> GetAvailableGateways();
}

/// <summary>
/// Implementation of payment gateway strategy resolver
/// </summary>
public class PaymentGatewayStrategy : IPaymentGatewayStrategy
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PaymentGatewaySettings _settings;
    private readonly ILogger<PaymentGatewayStrategy> _logger;
    private readonly Dictionary<string, Type> _gatewayTypes;
    private readonly Random _random = new();

    public PaymentGatewayStrategy(
        IServiceProvider serviceProvider,
        IOptions<PaymentGatewaySettings> settings,
        ILogger<PaymentGatewayStrategy> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;

        // Map gateway names to their implementation types
        _gatewayTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["Stripe"] = typeof(Payment.StripePaymentGateway),
            ["PayPal"] = typeof(Payment.PayPalPaymentGateway)
        };
    }

    /// <inheritdoc/>
    public IPaymentGateway GetGateway(PaymentSelectionContext context)
    {
        string? selectedGateway = null;

        // Strategy 1: Check game type-based gateway mapping
        if (!string.IsNullOrEmpty(context.GameType) && 
            _settings.GameTypeProviders.TryGetValue(context.GameType, out var gameGateway))
        {
            selectedGateway = gameGateway;
            _logger.LogInformation(
                "Selected payment gateway {Gateway} based on game type {GameType}",
                selectedGateway, context.GameType);
        }
        // Strategy 2: Check region-based gateway mapping
        else if (!string.IsNullOrEmpty(context.Region) && 
                 _settings.RegionProviders.TryGetValue(context.Region, out var regionGateway))
        {
            selectedGateway = regionGateway;
            _logger.LogInformation(
                "Selected payment gateway {Gateway} based on region {Region}",
                selectedGateway, context.Region);
        }
        // Strategy 3: A/B testing with weighted distribution
        else if (_settings.EnableABTesting && _settings.ABTestingWeights.Count > 0)
        {
            selectedGateway = SelectGatewayByWeight();
            _logger.LogInformation(
                "Selected payment gateway {Gateway} via A/B testing",
                selectedGateway);
        }
        // Strategy 4: Use default gateway
        else
        {
            selectedGateway = _settings.DefaultProvider;
            _logger.LogInformation(
                "Selected default payment gateway {Gateway}",
                selectedGateway);
        }

        return GetGatewayByName(selectedGateway);
    }

    /// <inheritdoc/>
    public IPaymentGateway GetGatewayByName(string gatewayName)
    {
        if (!_gatewayTypes.TryGetValue(gatewayName, out var gatewayType))
        {
            _logger.LogWarning(
                "Unknown payment gateway {Gateway}, falling back to {Default}",
                gatewayName, _settings.DefaultProvider);

            gatewayName = _settings.DefaultProvider;
            gatewayType = _gatewayTypes[gatewayName];
        }

        var gateway = _serviceProvider.GetService(gatewayType) as IPaymentGateway;
        
        if (gateway == null)
        {
            throw new InvalidOperationException(
                $"Payment gateway {gatewayName} is not registered in DI container");
        }

        return gateway;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAvailableGateways()
    {
        return _gatewayTypes.Keys;
    }

    #region Private Helper Methods

    private string SelectGatewayByWeight()
    {
        var totalWeight = _settings.ABTestingWeights.Values.Sum();
        var randomValue = _random.Next(0, totalWeight);

        var cumulativeWeight = 0;
        foreach (var kvp in _settings.ABTestingWeights)
        {
            cumulativeWeight += kvp.Value;
            if (randomValue < cumulativeWeight)
            {
                return kvp.Key;
            }
        }

        // Fallback (shouldn't reach here)
        return _settings.DefaultProvider;
    }

    #endregion
}

/// <summary>
/// Context information for payment gateway selection
/// </summary>
public class PaymentSelectionContext
{
    /// <summary>
    /// Game type (Minecraft, Valheim, etc.)
    /// </summary>
    public string? GameType { get; set; }

    /// <summary>
    /// User's region/country
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// User ID for tracking/analytics
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Payment amount for gateway selection
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Additional custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

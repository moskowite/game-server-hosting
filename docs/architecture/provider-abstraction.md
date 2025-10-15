# Provider Abstraction Architecture

## Overview

The Game Server Hosting platform now uses a **Provider Abstraction Pattern** to decouple the application from specific cloud and payment vendors. This architecture enables:

- **Multi-cloud support**: Seamlessly switch between DigitalOcean, AWS EC2, and Azure VM
- **Multi-payment gateway support**: Accept payments through Stripe, PayPal, and future gateways
- **Runtime provider selection**: Choose providers based on game type, region, plan, or A/B testing
- **Improved testability**: Mock providers without external API calls
- **Resilience**: Built-in retry, timeout, and circuit breaker policies using Polly

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│  (ServerProvisioningService, PaymentService, etc.)         │
└──────────────────┬────────────────────────┬─────────────────┘
                   │                        │
        ┌──────────▼──────────┐  ┌─────────▼──────────┐
        │ICloudProviderService│  │  IPaymentGateway   │
        │    (Interface)      │  │    (Interface)     │
        └──────────┬──────────┘  └─────────┬──────────┘
                   │                        │
        ┌──────────▼──────────┐  ┌─────────▼──────────┐
        │CloudProviderStrategy│  │PaymentGatewayStrategy│
        │  (Strategy Resolver)│  │  (Strategy Resolver)│
        └──────────┬──────────┘  └─────────┬──────────┘
                   │                        │
       ┌───────────┼───────────┐  ┌────────┼────────┐
       │           │           │  │        │        │
    ┌──▼──┐    ┌──▼──┐    ┌──▼──▼──┐  ┌──▼──┐  ┌──▼──┐
    │ DO  │    │ AWS │    │Azure│  │Stripe│  │PayPal│
    └─────┘    └─────┘    └─────┘  └──────┘  └──────┘
   (Adapter)  (Adapter)  (Adapter) (Adapter) (Adapter)
```

## Core Interfaces

### ICloudProviderService

Provider-agnostic interface for cloud infrastructure operations:

```csharp
public interface ICloudProviderService
{
    Task<ProvisionResult> CreateServerAsync(ServerProvisionRequest request);
    Task<ServerInfo> GetServerStatusAsync(string providerId);
    Task<bool> StartServerAsync(string providerId);
    Task<bool> StopServerAsync(string providerId);
    Task<bool> RestartServerAsync(string providerId);
    Task<bool> DeleteServerAsync(string providerId);
    Task<ServerMetrics> GetServerMetricsAsync(string providerId);
}
```

**Implementations:**
- `DigitalOceanProviderService` - Production-ready with full API integration
- `AwsEc2CloudProvider` - Placeholder implementation (ready for AWS SDK integration)
- `AzureVmCloudProvider` - Placeholder implementation (ready for Azure SDK integration)

### IPaymentGateway

Provider-agnostic interface for payment processing:

```csharp
public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CheckoutSessionRequest request);
    Task<PaymentIntentResult> CreatePaymentIntentAsync(PaymentIntentRequest request);
    Task<PaymentIntentResult> ConfirmPaymentAsync(string paymentIntentId, string? paymentMethodId = null);
    Task<PaymentIntentResult> CapturePaymentAsync(string paymentIntentId);
    Task<RefundResult> RefundPaymentAsync(RefundRequest request);
    WebhookEvent VerifyWebhook(string payload, string signature, string secret);
    Task<CustomerResult> CreateCustomerAsync(CustomerRequest request);
    Task<CustomerResult> GetCustomerAsync(string customerId);
}
```

**Implementations:**
- `StripePaymentGateway` - Production-ready with full Stripe API integration
- `PayPalPaymentGateway` - Basic implementation with REST API integration

## Strategy Pattern

### Cloud Provider Strategy

The `ICloudProviderStrategy` selects the appropriate cloud provider based on:

1. **Plan-based routing**: Premium plans → Azure, Standard → DigitalOcean
2. **Game type routing**: Minecraft → DigitalOcean, Valheim → AWS
3. **Region routing**: us-east → AWS, eu-west → Azure
4. **Default fallback**: Configurable default provider

Example usage:

```csharp
public class ServerProvisioningService
{
    private readonly ICloudProviderStrategy _cloudStrategy;
    
    public async Task<Guid> ProvisionServerAsync(Guid orderId)
    {
        var context = new ProviderSelectionContext
        {
            GameType = "Minecraft",
            Region = "nyc3",
            PlanName = "standard"
        };
        
        var provider = _cloudStrategy.GetProvider(context);
        var result = await provider.CreateServerAsync(request);
        // ...
    }
}
```

### Payment Gateway Strategy

The `IPaymentGatewayStrategy` selects the appropriate payment gateway based on:

1. **Game type routing**: Different games can use different gateways
2. **Region routing**: EU → Stripe, APAC → PayPal
3. **A/B Testing**: Weighted distribution (e.g., 80% Stripe, 20% PayPal)
4. **Default fallback**: Configurable default gateway

Example usage:

```csharp
public class CheckoutService
{
    private readonly IPaymentGatewayStrategy _gatewayStrategy;
    
    public async Task<string> CreateCheckoutAsync(CheckoutRequest request)
    {
        var context = new PaymentSelectionContext
        {
            GameType = request.GameType,
            Region = request.UserRegion,
            UserId = request.UserId
        };
        
        var gateway = _gatewayStrategy.GetGateway(context);
        var session = await gateway.CreateCheckoutSessionAsync(request);
        return session.CheckoutUrl;
    }
}
```

## Configuration

### appsettings.json Structure

```json
{
  "CloudProviders": {
    "DefaultProvider": "DigitalOcean",
    "GameTypeProviders": {
      "Minecraft": "DigitalOcean",
      "Valheim": "AWS",
      "Rust": "Azure"
    },
    "RegionProviders": {
      "us-east": "AWS",
      "eu-west": "Azure",
      "nyc": "DigitalOcean"
    },
    "PlanProviders": {
      "enterprise": "Azure",
      "premium": "AWS",
      "standard": "DigitalOcean"
    },
    "DigitalOcean": { /* settings */ },
    "AWS": { /* settings */ },
    "Azure": { /* settings */ }
  },
  "PaymentGateways": {
    "DefaultProvider": "Stripe",
    "GameTypeProviders": { /* mapping */ },
    "RegionProviders": { /* mapping */ },
    "EnableABTesting": false,
    "ABTestingWeights": {
      "Stripe": 80,
      "PayPal": 20
    },
    "PayPal": { /* settings */ }
  }
}
```

### Environment Variables / User Secrets

Sensitive credentials should be stored in user secrets or environment variables:

```bash
# DigitalOcean
CloudProviders__DigitalOcean__ApiToken="dop_v1_..."

# AWS
CloudProviders__AWS__AccessKeyId="AKIA..."
CloudProviders__AWS__SecretAccessKey="..."

# Azure
CloudProviders__Azure__ClientId="..."
CloudProviders__Azure__ClientSecret="..."
CloudProviders__Azure__TenantId="..."
CloudProviders__Azure__SubscriptionId="..."

# Stripe
Stripe__SecretKey="sk_test_..."
Stripe__PublishableKey="pk_test_..."
Stripe__WebhookSecret="whsec_..."

# PayPal
PaymentGateways__PayPal__ClientId="..."
PaymentGateways__PayPal__ClientSecret="..."
```

## Resilience Policies

All HTTP-based providers are configured with Polly resilience policies:

### Retry Policy
- **Retries**: 3 attempts
- **Backoff**: Exponential (2s, 4s, 8s)
- **Triggers**: Transient HTTP errors (5xx, timeouts, network errors)

### Circuit Breaker
- **Threshold**: 5 consecutive failures
- **Break Duration**: 30 seconds
- **Purpose**: Prevent cascading failures

### Timeout Policy
- **Default**: 30 seconds per request
- **Purpose**: Prevent hanging requests

Example configuration:

```csharp
services.AddHttpClient<DigitalOceanProviderService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy(TimeSpan.FromSeconds(30)));
```

## Adding a New Provider

### Adding a New Cloud Provider

1. **Create the adapter class**:

```csharp
public class GoogleCloudProvider : ICloudProviderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleCloudProvider> _logger;
    private readonly GoogleCloudSettings _settings;
    
    // Implement interface methods...
}
```

2. **Create settings class**:

```csharp
public class GoogleCloudSettings
{
    public string ProjectId { get; set; } = string.Empty;
    public string ServiceAccountKey { get; set; } = string.Empty;
    public string DefaultRegion { get; set; } = "us-central1";
    // ...
}
```

3. **Register in DI** (`ProviderServiceExtensions.cs`):

```csharp
services.AddHttpClient<GoogleCloudProvider>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy(TimeSpan.FromSeconds(30)));
```

4. **Update strategy resolver** (`CloudProviderStrategy.cs`):

```csharp
_providerTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
{
    ["DigitalOcean"] = typeof(Services.DigitalOceanProviderService),
    ["AWS"] = typeof(Cloud.AwsEc2CloudProvider),
    ["Azure"] = typeof(Cloud.AzureVmCloudProvider),
    ["GCP"] = typeof(Cloud.GoogleCloudProvider)  // Add this
};
```

5. **Add configuration** to `appsettings.json`:

```json
{
  "CloudProviders": {
    "GCP": {
      "ProjectId": "",
      "ServiceAccountKey": "",
      "DefaultRegion": "us-central1"
    }
  }
}
```

### Adding a New Payment Gateway

Follow the same pattern, implementing `IPaymentGateway` instead.

## Testing

### Unit Testing with Mocked Providers

```csharp
[Fact]
public async Task ProvisionServer_UsesCorrectProvider_ForGameType()
{
    // Arrange
    var mockProvider = new Mock<ICloudProviderService>();
    mockProvider
        .Setup(p => p.CreateServerAsync(It.IsAny<ServerProvisionRequest>()))
        .ReturnsAsync(new ProvisionResult { Success = true });
    
    var mockStrategy = new Mock<ICloudProviderStrategy>();
    mockStrategy
        .Setup(s => s.GetProvider(It.IsAny<ProviderSelectionContext>()))
        .Returns(mockProvider.Object);
    
    var service = new ServerProvisioningService(
        context, mockStrategy.Object, logger, emailService, settings);
    
    // Act
    var result = await service.ProvisionServerAsync(orderId);
    
    // Assert
    mockProvider.Verify(p => p.CreateServerAsync(It.IsAny<ServerProvisionRequest>()), Times.Once);
}
```

### Integration Testing

See test project for examples of integration tests using actual provider sandbox environments.

## Migration Guide

### For Existing Code

The good news: **ServerProvisioningService already uses ICloudProviderService**, so no changes needed!

For payment processing:

**Before**:
```csharp
public class PaymentService
{
    private readonly IStripeService _stripe;
    
    public async Task<PaymentIntent> CreatePaymentAsync(...)
    {
        return await _stripe.CreatePaymentIntentAsync(...);
    }
}
```

**After**:
```csharp
public class PaymentService
{
    private readonly IPaymentGatewayStrategy _gatewayStrategy;
    
    public async Task<PaymentIntentResult> CreatePaymentAsync(...)
    {
        var gateway = _gatewayStrategy.GetGateway(context);
        return await gateway.CreatePaymentIntentAsync(...);
    }
}
```

## Performance Considerations

- **Strategy Resolution**: Cached singleton instances, minimal overhead
- **HTTP Clients**: Reused via typed HttpClient pattern
- **Circuit Breaker**: Prevents wasted requests to failing providers
- **Retry Policy**: Exponential backoff prevents thundering herd

## Security Best Practices

1. **Never commit secrets**: Use user-secrets, environment variables, or Azure Key Vault
2. **Rotate credentials regularly**: Especially for production environments
3. **Use least-privilege IAM**: Grant only necessary permissions to service accounts
4. **Validate webhook signatures**: Always verify webhook authenticity
5. **Encrypt sensitive data**: Use encryption at rest for stored credentials

## Future Enhancements

- **Multi-region failover**: Automatically failover to backup region on failure
- **Cost optimization**: Route to cheapest provider based on current spot prices
- **Provider health monitoring**: Track success rates and automatically disable unhealthy providers
- **Advanced A/B testing**: Per-user consistent routing, gradual rollouts
- **Provider capability discovery**: Dynamic feature detection and graceful degradation

## References

- [Stripe API Documentation](https://stripe.com/docs/api)
- [PayPal REST API](https://developer.paypal.com/docs/api/overview/)
- [DigitalOcean API](https://docs.digitalocean.com/reference/api/)
- [AWS EC2 Documentation](https://docs.aws.amazon.com/ec2/)
- [Azure VM Documentation](https://docs.microsoft.com/azure/virtual-machines/)
- [Polly Resilience Policies](https://github.com/App-vNext/Polly)

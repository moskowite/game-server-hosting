# Provider Abstraction Quick Reference

## Quick Start

### Using Cloud Providers

```csharp
// Option 1: Auto-select via strategy
public class ServerProvisioningService
{
    private readonly ICloudProviderStrategy _strategy;
    
    public async Task ProvisionAsync(Guid orderId)
    {
        var context = new ProviderSelectionContext
        {
            GameType = "Minecraft",
            Region = "nyc3"
        };
        
        var provider = _strategy.GetProvider(context);
        var result = await provider.CreateServerAsync(request);
    }
}

// Option 2: Direct provider selection
public class ServerProvisioningService
{
    private readonly ICloudProviderStrategy _strategy;
    
    public async Task ProvisionOnAwsAsync(ServerProvisionRequest request)
    {
        var provider = _strategy.GetProviderByName("AWS");
        var result = await provider.CreateServerAsync(request);
    }
}
```

### Using Payment Gateways

```csharp
// Option 1: Auto-select via strategy
public class CheckoutService
{
    private readonly IPaymentGatewayStrategy _strategy;
    
    public async Task<string> CreateCheckoutAsync(CheckoutRequest request)
    {
        var context = new PaymentSelectionContext
        {
            UserId = request.UserId,
            Region = request.UserRegion
        };
        
        var gateway = _strategy.GetGateway(context);
        var session = await gateway.CreateCheckoutSessionAsync(checkoutRequest);
        return session.CheckoutUrl;
    }
}

// Option 2: Direct gateway selection
public class CheckoutService
{
    private readonly IPaymentGatewayStrategy _strategy;
    
    public async Task<string> CreatePayPalCheckoutAsync(CheckoutRequest request)
    {
        var gateway = _strategy.GetGatewayByName("PayPal");
        var session = await gateway.CreateCheckoutSessionAsync(checkoutRequest);
        return session.CheckoutUrl;
    }
}
```

## Configuration Cheat Sheet

### Switching Default Provider

```json
{
  "CloudProviders": {
    "DefaultProvider": "AWS"  // Change from "DigitalOcean" to "AWS"
  }
}
```

### Game-Specific Routing

```json
{
  "CloudProviders": {
    "GameTypeProviders": {
      "Minecraft": "DigitalOcean",
      "Valheim": "AWS",
      "Rust": "Azure",
      "ARK": "DigitalOcean"
    }
  }
}
```

### A/B Testing Payments

```json
{
  "PaymentGateways": {
    "EnableABTesting": true,
    "ABTestingWeights": {
      "Stripe": 70,
      "PayPal": 30
    }
  }
}
```

## Common Operations

### Create a Server

```csharp
var request = new ServerProvisionRequest
{
    ServerId = Guid.NewGuid(),
    UserId = userId,
    ServerType = "Minecraft",
    ServerName = "My Server",
    RamMb = 4096,
    CpuCores = 2,
    StorageGb = 50,
    Region = "nyc3"
};

var provider = _cloudStrategy.GetProvider(new ProviderSelectionContext 
{ 
    GameType = "Minecraft" 
});

var result = await provider.CreateServerAsync(request);

if (result.Success)
{
    Console.WriteLine($"Server created at {result.IpAddress}:{result.Port}");
}
```

### Create a Payment

```csharp
var request = new PaymentIntentRequest
{
    Amount = 2999, // $29.99 in cents
    Currency = "usd",
    Description = "Minecraft Server - Standard Plan",
    Metadata = new Dictionary<string, string>
    {
        ["order_id"] = orderId.ToString(),
        ["user_id"] = userId.ToString()
    }
};

var gateway = _paymentStrategy.GetGateway(new PaymentSelectionContext
{
    UserId = userId
});

var paymentIntent = await gateway.CreatePaymentIntentAsync(request);
Console.WriteLine($"Client Secret: {paymentIntent.ClientSecret}");
```

### Refund a Payment

```csharp
var refundRequest = new RefundRequest
{
    PaymentIntentId = "pi_123456",
    Amount = 2999, // Full refund
    Reason = "Customer request"
};

var gateway = _paymentStrategy.GetGatewayByName("Stripe");
var refund = await gateway.RefundPaymentAsync(refundRequest);

Console.WriteLine($"Refund {refund.RefundId} status: {refund.Status}");
```

## Provider Capabilities Matrix

| Feature | DigitalOcean | AWS | Azure | Status |
|---------|--------------|-----|-------|--------|
| Create Server | ✅ | ⚠️ | ⚠️ | DO: Production, AWS/Azure: Placeholder |
| Delete Server | ✅ | ⚠️ | ⚠️ | DO: Production, AWS/Azure: Placeholder |
| Start/Stop | ✅ | ⚠️ | ⚠️ | DO: Production, AWS/Azure: Placeholder |
| Metrics | ⚠️ | ⚠️ | ⚠️ | All: Placeholder |
| Backups | ✅ | ❌ | ❌ | Only DO |

| Feature | Stripe | PayPal | Status |
|---------|--------|--------|--------|
| Checkout Session | ✅ | ✅ | Both: Production-ready |
| Payment Intent | ✅ | ⚠️ | Stripe: Full, PayPal: Basic |
| Refunds | ✅ | ✅ | Both: Production-ready |
| Webhooks | ✅ | ⚠️ | Stripe: Full, PayPal: Basic |
| Subscriptions | ❌ | ❌ | Not implemented |

**Legend**: ✅ Production-ready | ⚠️ Placeholder/Basic | ❌ Not implemented

## Troubleshooting

### Provider Not Found Error

**Error**: `InvalidOperationException: Cloud provider AWS is not registered in DI container`

**Solution**: Ensure provider is registered in `ProviderServiceExtensions.cs`

```csharp
services.AddHttpClient<AwsEc2CloudProvider>()
    .AddPolicyHandler(GetRetryPolicy());
```

### Configuration Not Loading

**Error**: Settings are empty or default values

**Solution**: Check configuration section names match exactly:

```json
{
  "CloudProviders": {           // Exact case
    "AWS": {                    // Exact case
      "AccessKeyId": "..."
    }
  }
}
```

### Circuit Breaker Opened

**Error**: Requests failing with circuit breaker open error

**Solution**: Provider is experiencing repeated failures. Check:
1. API credentials are valid
2. Network connectivity
3. Provider API status page
4. Wait 30 seconds for circuit to close automatically

### Polly Retry Exhausted

**Error**: All retry attempts failed

**Solution**: 
1. Check provider API logs
2. Verify credentials
3. Check rate limits
4. Review Polly policy configuration

## Environment-Specific Configuration

### Development

```json
{
  "CloudProviders": {
    "DefaultProvider": "DigitalOcean",
    "DigitalOcean": {
      "ApiToken": "dop_v1_dev_token"
    }
  },
  "PaymentGateways": {
    "DefaultProvider": "Stripe",
    "PayPal": {
      "Mode": "sandbox",
      "ApiBaseUrl": "https://api-m.sandbox.paypal.com"
    }
  },
  "Stripe": {
    "SecretKey": "sk_test_..."
  }
}
```

### Production

Use environment variables or Azure Key Vault:

```bash
CloudProviders__DigitalOcean__ApiToken="dop_v1_prod_token"
CloudProviders__AWS__AccessKeyId="AKIA..."
PaymentGateways__PayPal__Mode="live"
PaymentGateways__PayPal__ApiBaseUrl="https://api-m.paypal.com"
Stripe__SecretKey="sk_live_..."
```

## Testing

### Mock Providers

```csharp
var mockProvider = new Mock<ICloudProviderService>();
mockProvider
    .Setup(p => p.CreateServerAsync(It.IsAny<ServerProvisionRequest>()))
    .ReturnsAsync(new ProvisionResult { Success = true, IpAddress = "192.0.2.1" });

var mockStrategy = new Mock<ICloudProviderStrategy>();
mockStrategy
    .Setup(s => s.GetProvider(It.IsAny<ProviderSelectionContext>()))
    .Returns(mockProvider.Object);
```

### Integration Tests

Use sandbox environments:
- **Stripe**: Test mode keys (`sk_test_...`)
- **PayPal**: Sandbox mode (`https://api-m.sandbox.paypal.com`)
- **DigitalOcean**: Test API token with limited quota

## Performance Tips

1. **Reuse HttpClients**: Already configured via typed HttpClient pattern
2. **Cache strategy instances**: Strategies are registered as singletons
3. **Use async/await**: All provider methods are async
4. **Batch operations**: Where supported by provider API
5. **Monitor circuit breakers**: Track open/closed state for alerting

## Security Checklist

- [ ] Secrets stored in user-secrets/Key Vault, not appsettings.json
- [ ] Production keys use `sk_live_`, `dop_v1_`, etc. (not test keys)
- [ ] Webhook signatures verified before processing
- [ ] API keys rotated regularly
- [ ] Service accounts use least-privilege IAM policies
- [ ] HTTPS enforced for all provider API calls
- [ ] PCI compliance for payment data handling

## Quick Links

- [Full Architecture Documentation](../architecture/provider-abstraction.md)
- [Stripe Testing](https://stripe.com/docs/testing)
- [PayPal Sandbox](https://developer.paypal.com/developer/accounts/)
- [DigitalOcean API](https://docs.digitalocean.com/reference/api/)
- [Polly Documentation](https://github.com/App-vNext/Polly)

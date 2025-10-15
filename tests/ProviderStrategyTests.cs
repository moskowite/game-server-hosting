using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Infrastructure.Providers.Strategies;
using GameServerHosting.Infrastructure.Providers.Cloud;
using GameServerHosting.Infrastructure.Providers.Payment;

namespace GameServerHosting.Tests.Unit.Providers;

/// <summary>
/// Unit tests for provider strategy resolution
/// </summary>
public class CloudProviderStrategyTests
{
    [Fact]
    public void GetProvider_ReturnsDigitalOcean_WhenGameTypeIsMinecraft()
    {
        // Arrange
        var settings = new CloudProviderSelectionSettings
        {
            DefaultProvider = "DigitalOcean",
            GameTypeProviders = new Dictionary<string, string>
            {
                ["Minecraft"] = "DigitalOcean",
                ["Valheim"] = "AWS"
            }
        };

        var services = new ServiceCollection();
        services.AddSingleton<DigitalOceanProviderService>();
        services.AddSingleton<AwsEc2CloudProvider>();
        services.AddSingleton<AzureVmCloudProvider>();
        
        // Mock dependencies for each provider
        services.AddSingleton(Mock.Of<ILogger<DigitalOceanProviderService>>());
        services.AddSingleton(Mock.Of<ILogger<AwsEc2CloudProvider>>());
        services.AddSingleton(Mock.Of<ILogger<AzureVmCloudProvider>>());
        services.AddSingleton(Mock.Of<HttpClient>());
        services.AddSingleton(Options.Create(new DigitalOceanSettings()));
        services.AddSingleton(Options.Create(new AwsEc2Settings()));
        services.AddSingleton(Options.Create(new AzureVmSettings()));

        var serviceProvider = services.BuildServiceProvider();
        
        var strategy = new CloudProviderStrategy(
            serviceProvider,
            Options.Create(settings),
            Mock.Of<ILogger<CloudProviderStrategy>>());

        var context = new ProviderSelectionContext
        {
            GameType = "Minecraft"
        };

        // Act
        var provider = strategy.GetProvider(context);

        // Assert
        Assert.IsType<DigitalOceanProviderService>(provider);
    }

    [Fact]
    public void GetProvider_ReturnsAWS_WhenRegionIsUsEast()
    {
        // Arrange
        var settings = new CloudProviderSelectionSettings
        {
            DefaultProvider = "DigitalOcean",
            RegionProviders = new Dictionary<string, string>
            {
                ["us-east"] = "AWS",
                ["eu-west"] = "Azure"
            }
        };

        var services = new ServiceCollection();
        services.AddSingleton<DigitalOceanProviderService>();
        services.AddSingleton<AwsEc2CloudProvider>();
        
        // Mock dependencies
        services.AddSingleton(Mock.Of<ILogger<DigitalOceanProviderService>>());
        services.AddSingleton(Mock.Of<ILogger<AwsEc2CloudProvider>>());
        services.AddSingleton(Mock.Of<HttpClient>());
        services.AddSingleton(Options.Create(new DigitalOceanSettings()));
        services.AddSingleton(Options.Create(new AwsEc2Settings()));

        var serviceProvider = services.BuildServiceProvider();
        
        var strategy = new CloudProviderStrategy(
            serviceProvider,
            Options.Create(settings),
            Mock.Of<ILogger<CloudProviderStrategy>>());

        var context = new ProviderSelectionContext
        {
            Region = "us-east"
        };

        // Act
        var provider = strategy.GetProvider(context);

        // Assert
        Assert.IsType<AwsEc2CloudProvider>(provider);
    }

    [Fact]
    public void GetProviderByName_ReturnsCorrectProvider_WhenNameIsValid()
    {
        // Arrange
        var settings = new CloudProviderSelectionSettings
        {
            DefaultProvider = "DigitalOcean"
        };

        var services = new ServiceCollection();
        services.AddSingleton<AzureVmCloudProvider>();
        services.AddSingleton(Mock.Of<ILogger<AzureVmCloudProvider>>());
        services.AddSingleton(Mock.Of<HttpClient>());
        services.AddSingleton(Options.Create(new AzureVmSettings()));

        var serviceProvider = services.BuildServiceProvider();
        
        var strategy = new CloudProviderStrategy(
            serviceProvider,
            Options.Create(settings),
            Mock.Of<ILogger<CloudProviderStrategy>>());

        // Act
        var provider = strategy.GetProviderByName("Azure");

        // Assert
        Assert.IsType<AzureVmCloudProvider>(provider);
    }
}

public class PaymentGatewayStrategyTests
{
    [Fact]
    public void GetGateway_ReturnsStripe_ByDefault()
    {
        // Arrange
        var settings = new PaymentGatewaySettings
        {
            DefaultProvider = "Stripe",
            EnableABTesting = false
        };

        var services = new ServiceCollection();
        services.AddSingleton<StripePaymentGateway>();
        services.AddSingleton(Mock.Of<ILogger<StripePaymentGateway>>());
        services.AddSingleton(Mock.Of<HttpClient>());
        services.AddSingleton(Options.Create(new StripeSettings()));

        var serviceProvider = services.BuildServiceProvider();
        
        var strategy = new PaymentGatewayStrategy(
            serviceProvider,
            Options.Create(settings),
            Mock.Of<ILogger<PaymentGatewayStrategy>>());

        var context = new PaymentSelectionContext();

        // Act
        var gateway = strategy.GetGateway(context);

        // Assert
        Assert.IsType<StripePaymentGateway>(gateway);
        Assert.Equal("Stripe", gateway.ProviderName);
    }

    [Fact]
    public void GetGateway_ReturnsPayPal_WhenGameTypeMatches()
    {
        // Arrange
        var settings = new PaymentGatewaySettings
        {
            DefaultProvider = "Stripe",
            GameTypeProviders = new Dictionary<string, string>
            {
                ["Valheim"] = "PayPal"
            },
            EnableABTesting = false
        };

        var services = new ServiceCollection();
        services.AddSingleton<PayPalPaymentGateway>();
        services.AddSingleton(Mock.Of<ILogger<PayPalPaymentGateway>>());
        services.AddSingleton(Mock.Of<HttpClient>());
        services.AddSingleton(Options.Create(new PayPalSettings()));

        var serviceProvider = services.BuildServiceProvider();
        
        var strategy = new PaymentGatewayStrategy(
            serviceProvider,
            Options.Create(settings),
            Mock.Of<ILogger<PaymentGatewayStrategy>>());

        var context = new PaymentSelectionContext
        {
            GameType = "Valheim"
        };

        // Act
        var gateway = strategy.GetGateway(context);

        // Assert
        Assert.IsType<PayPalPaymentGateway>(gateway);
        Assert.Equal("PayPal", gateway.ProviderName);
    }

    [Fact]
    public void GetAvailableGateways_ReturnsAllRegisteredGateways()
    {
        // Arrange
        var settings = new PaymentGatewaySettings
        {
            DefaultProvider = "Stripe"
        };

        var strategy = new PaymentGatewayStrategy(
            Mock.Of<IServiceProvider>(),
            Options.Create(settings),
            Mock.Of<ILogger<PaymentGatewayStrategy>>());

        // Act
        var gateways = strategy.GetAvailableGateways();

        // Assert
        Assert.Contains("Stripe", gateways);
        Assert.Contains("PayPal", gateways);
        Assert.Equal(2, gateways.Count());
    }
}

/// <summary>
/// Integration tests demonstrating provider usage
/// </summary>
public class ProviderIntegrationExamples
{
    [Fact(Skip = "Example test - requires live credentials")]
    public async Task Example_CreateServer_UsingStrategyPattern()
    {
        // This is an example showing how the pattern works
        // In real tests, you would use mocks or test credentials
        
        var settings = new CloudProviderSelectionSettings
        {
            DefaultProvider = "DigitalOcean",
            GameTypeProviders = new Dictionary<string, string>
            {
                ["Minecraft"] = "DigitalOcean"
            }
        };

        // Setup DI container (simplified)
        var services = new ServiceCollection();
        // ... configure services ...
        var serviceProvider = services.BuildServiceProvider();

        var strategy = serviceProvider.GetRequiredService<ICloudProviderStrategy>();
        
        var context = new ProviderSelectionContext
        {
            GameType = "Minecraft",
            Region = "nyc3"
        };

        var provider = strategy.GetProvider(context);
        
        var request = new ServerProvisionRequest
        {
            ServerId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ServerType = "Minecraft",
            ServerName = "Test Server",
            RamMb = 2048,
            CpuCores = 1,
            StorageGb = 50
        };

        // Act
        var result = await provider.CreateServerAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.IpAddress);
    }
}

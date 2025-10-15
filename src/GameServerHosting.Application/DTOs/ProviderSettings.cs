namespace GameServerHosting.Application.DTOs;

/// <summary>
/// AWS EC2 cloud provider configuration
/// </summary>
public class AwsEc2Settings
{
    /// <summary>
    /// AWS Access Key ID
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// AWS Secret Access Key
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Default AWS region (e.g., us-east-1, eu-west-1)
    /// </summary>
    public string DefaultRegion { get; set; } = "us-east-1";

    /// <summary>
    /// AMI ID for server images (Ubuntu, Docker, etc.)
    /// </summary>
    public string DefaultAmiId { get; set; } = string.Empty;

    /// <summary>
    /// Security group ID for game servers
    /// </summary>
    public string? SecurityGroupId { get; set; }

    /// <summary>
    /// Key pair name for SSH access
    /// </summary>
    public string? KeyPairName { get; set; }

    /// <summary>
    /// Subnet ID for VPC placement
    /// </summary>
    public string? SubnetId { get; set; }

    /// <summary>
    /// Enable detailed monitoring
    /// </summary>
    public bool EnableDetailedMonitoring { get; set; } = true;

    /// <summary>
    /// Default tags to apply to instances
    /// </summary>
    public Dictionary<string, string> DefaultTags { get; set; } = new() { { "Application", "GameServerHosting" } };

    /// <summary>
    /// Timeout for provisioning operations in seconds
    /// </summary>
    public int ProvisioningTimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Azure VM cloud provider configuration
/// </summary>
public class AzureVmSettings
{
    /// <summary>
    /// Azure Subscription ID
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Azure Tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure Client ID (Application ID)
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Resource Group name for game servers
    /// </summary>
    public string ResourceGroupName { get; set; } = string.Empty;

    /// <summary>
    /// Default Azure region (e.g., eastus, westeurope)
    /// </summary>
    public string DefaultRegion { get; set; } = "eastus";

    /// <summary>
    /// VM Image reference (Publisher:Offer:Sku:Version)
    /// </summary>
    public string DefaultImageReference { get; set; } = "Canonical:UbuntuServer:18.04-LTS:latest";

    /// <summary>
    /// Virtual Network name
    /// </summary>
    public string? VirtualNetworkName { get; set; }

    /// <summary>
    /// Subnet name
    /// </summary>
    public string? SubnetName { get; set; }

    /// <summary>
    /// Network Security Group name
    /// </summary>
    public string? NetworkSecurityGroup { get; set; }

    /// <summary>
    /// Default tags to apply to resources
    /// </summary>
    public Dictionary<string, string> DefaultTags { get; set; } = new() { { "Application", "GameServerHosting" } };

    /// <summary>
    /// Timeout for provisioning operations in seconds
    /// </summary>
    public int ProvisioningTimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// PayPal payment gateway configuration
/// </summary>
public class PayPalSettings
{
    /// <summary>
    /// PayPal Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// PayPal Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// PayPal mode (sandbox or live)
    /// </summary>
    public string Mode { get; set; } = "sandbox";

    /// <summary>
    /// PayPal API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api-m.sandbox.paypal.com";

    /// <summary>
    /// Default currency for payments
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Success redirect URL
    /// </summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>
    /// Cancel redirect URL
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Webhook ID for event verification
    /// </summary>
    public string? WebhookId { get; set; }
}

/// <summary>
/// Payment gateway provider selection settings
/// </summary>
public class PaymentGatewaySettings
{
    /// <summary>
    /// Default payment gateway (Stripe, PayPal, etc.)
    /// </summary>
    public string DefaultProvider { get; set; } = "Stripe";

    /// <summary>
    /// Provider selection strategy per game type
    /// </summary>
    public Dictionary<string, string> GameTypeProviders { get; set; } = new();

    /// <summary>
    /// Provider selection strategy per region
    /// </summary>
    public Dictionary<string, string> RegionProviders { get; set; } = new();

    /// <summary>
    /// Enable A/B testing with percentage routing
    /// </summary>
    public bool EnableABTesting { get; set; } = false;

    /// <summary>
    /// A/B testing provider weights (provider name -> percentage)
    /// </summary>
    public Dictionary<string, int> ABTestingWeights { get; set; } = new();
}

/// <summary>
/// Cloud provider selection settings
/// </summary>
public class CloudProviderSelectionSettings
{
    /// <summary>
    /// Default cloud provider (DigitalOcean, AWS, Azure)
    /// </summary>
    public string DefaultProvider { get; set; } = "DigitalOcean";

    /// <summary>
    /// Provider selection strategy per game type
    /// </summary>
    public Dictionary<string, string> GameTypeProviders { get; set; } = new();

    /// <summary>
    /// Provider selection strategy per region
    /// </summary>
    public Dictionary<string, string> RegionProviders { get; set; } = new();

    /// <summary>
    /// Provider selection strategy per user tier/plan
    /// </summary>
    public Dictionary<string, string> PlanProviders { get; set; } = new();
}

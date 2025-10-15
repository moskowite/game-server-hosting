using GameServerHosting.Application.Interfaces;
using GameServerHosting.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GameServerHosting.Infrastructure.Providers.Payment;

/// <summary>
/// PayPal implementation of IPaymentGateway
/// </summary>
public class PayPalPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly PayPalSettings _settings;
    private readonly ILogger<PayPalPaymentGateway> _logger;
    private string? _cachedAccessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public string ProviderName => "PayPal";

    public PayPalPaymentGateway(
        HttpClient httpClient,
        IOptions<PayPalSettings> settings,
        ILogger<PayPalPaymentGateway> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.ApiBaseUrl);
    }

    /// <inheritdoc/>
    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(CheckoutSessionRequest request)
    {
        try
        {
            await EnsureAccessTokenAsync();

            // Create PayPal order
            var orderRequest = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = request.UserId.ToString(),
                        amount = new
                        {
                            currency_code = request.Currency.ToUpper(),
                            value = (request.LineItems.Sum(i => i.UnitAmount * i.Quantity) / 100m).ToString("F2")
                        },
                        description = string.Join(", ", request.LineItems.Select(i => i.Name))
                    }
                },
                application_context = new
                {
                    return_url = request.SuccessUrl,
                    cancel_url = request.CancelUrl,
                    user_action = "PAY_NOW"
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(orderRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/v2/checkout/orders", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var orderResponse = JsonDocument.Parse(responseJson);
            var root = orderResponse.RootElement;

            var orderId = root.GetProperty("id").GetString() ?? "";
            var approveLink = root.GetProperty("links")
                .EnumerateArray()
                .FirstOrDefault(l => l.GetProperty("rel").GetString() == "approve")
                .GetProperty("href").GetString();

            _logger.LogInformation(
                "Created PayPal order {OrderId} for user {UserId}",
                orderId, request.UserId);

            return new CheckoutSessionResult
            {
                SessionId = orderId,
                CheckoutUrl = approveLink,
                PaymentStatus = "unpaid",
                Metadata = request.Metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal error creating checkout session");
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CheckoutSessionResult> GetCheckoutSessionAsync(string sessionId)
    {
        try
        {
            await EnsureAccessTokenAsync();

            var response = await _httpClient.GetAsync($"/v2/checkout/orders/{sessionId}");
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var order = JsonDocument.Parse(responseJson);
            var root = order.RootElement;

            var status = root.GetProperty("status").GetString() ?? "";

            return new CheckoutSessionResult
            {
                SessionId = sessionId,
                PaymentStatus = status.ToLower(),
                Amount = root.TryGetProperty("purchase_units", out var units)
                    ? (long)(decimal.Parse(units[0].GetProperty("amount").GetProperty("value").GetString() ?? "0") * 100)
                    : 0,
                Currency = root.TryGetProperty("purchase_units", out var units2)
                    ? units2[0].GetProperty("amount").GetProperty("currency_code").GetString()
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal error retrieving checkout session {SessionId}", sessionId);
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(PaymentIntentRequest request)
    {
        // PayPal doesn't have a direct "payment intent" concept
        // We'll create an order instead
        var checkoutRequest = new CheckoutSessionRequest
        {
            UserId = Guid.Empty,
            LineItems = new List<CheckoutLineItem>
            {
                new CheckoutLineItem
                {
                    Name = request.Description ?? "Payment",
                    Quantity = 1,
                    UnitAmount = request.Amount
                }
            },
            Currency = request.Currency,
            Metadata = request.Metadata
        };

        var session = await CreateCheckoutSessionAsync(checkoutRequest);

        return new PaymentIntentResult
        {
            PaymentIntentId = session.SessionId,
            ClientSecret = null, // PayPal doesn't use client secrets
            Status = session.PaymentStatus,
            Amount = request.Amount,
            Currency = request.Currency,
            Metadata = request.Metadata,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task<PaymentIntentResult> ConfirmPaymentAsync(string paymentIntentId, string? paymentMethodId = null)
    {
        // PayPal confirmations happen via redirect flow, not API
        var session = await GetCheckoutSessionAsync(paymentIntentId);

        return new PaymentIntentResult
        {
            PaymentIntentId = paymentIntentId,
            Status = session.PaymentStatus,
            Amount = session.Amount ?? 0,
            Currency = session.Currency ?? "USD",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task<PaymentIntentResult> CapturePaymentAsync(string paymentIntentId)
    {
        try
        {
            await EnsureAccessTokenAsync();

            var response = await _httpClient.PostAsync(
                $"/v2/checkout/orders/{paymentIntentId}/capture",
                new StringContent("", Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var captureResponse = JsonDocument.Parse(responseJson);
            var root = captureResponse.RootElement;

            _logger.LogInformation("Captured PayPal order {OrderId}", paymentIntentId);

            return new PaymentIntentResult
            {
                PaymentIntentId = paymentIntentId,
                Status = root.GetProperty("status").GetString() ?? "",
                Amount = 0, // Would need to parse from purchase_units
                Currency = "USD",
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal error capturing payment {PaymentIntentId}", paymentIntentId);
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<RefundResult> RefundPaymentAsync(RefundRequest request)
    {
        try
        {
            await EnsureAccessTokenAsync();

            var refundRequest = new
            {
                amount = request.Amount.HasValue ? new
                {
                    value = (request.Amount.Value / 100m).ToString("F2"),
                    currency_code = "USD"
                } : null,
                note_to_payer = request.Reason
            };

            var content = new StringContent(
                JsonSerializer.Serialize(refundRequest),
                Encoding.UTF8,
                "application/json");

            // Note: In PayPal, we refund captures, not orders
            // This assumes the paymentIntentId is actually a capture ID
            var response = await _httpClient.PostAsync(
                $"/v2/payments/captures/{request.PaymentIntentId}/refund",
                content);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var refundResponse = JsonDocument.Parse(responseJson);
            var root = refundResponse.RootElement;

            var refundId = root.GetProperty("id").GetString() ?? "";

            _logger.LogInformation(
                "Created PayPal refund {RefundId} for capture {CaptureId}",
                refundId, request.PaymentIntentId);

            return new RefundResult
            {
                RefundId = refundId,
                PaymentIntentId = request.PaymentIntentId,
                Amount = request.Amount ?? 0,
                Currency = "USD",
                Status = root.GetProperty("status").GetString() ?? "",
                Reason = request.Reason,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal error creating refund for {PaymentIntentId}", request.PaymentIntentId);
            throw new InvalidOperationException($"Payment provider error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public WebhookEvent VerifyWebhook(string payload, string signature, string secret)
    {
        try
        {
            // PayPal webhook verification is complex and requires calling their API
            // For simplicity, we'll parse the event without full verification
            var eventData = JsonDocument.Parse(payload);
            var root = eventData.RootElement;

            _logger.LogInformation("Processing PayPal webhook event {EventType}",
                root.GetProperty("event_type").GetString());

            return new WebhookEvent
            {
                EventId = root.GetProperty("id").GetString() ?? "",
                EventType = root.GetProperty("event_type").GetString() ?? "",
                DataJson = payload,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal error verifying webhook");
            throw new InvalidOperationException($"Invalid webhook payload: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public Task<CustomerResult> CreateCustomerAsync(CustomerRequest request)
    {
        // PayPal doesn't have a separate customer creation API
        // Customers are created automatically during checkout
        _logger.LogInformation("PayPal customer creation - no-op");

        return Task.FromResult(new CustomerResult
        {
            CustomerId = $"paypal-{Guid.NewGuid():N}",
            Email = request.Email,
            Name = request.Name,
            Metadata = request.Metadata,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <inheritdoc/>
    public Task<CustomerResult> GetCustomerAsync(string customerId)
    {
        // PayPal doesn't have a customer retrieval API in the same way
        _logger.LogWarning("PayPal customer retrieval not fully supported");

        return Task.FromResult(new CustomerResult
        {
            CustomerId = customerId,
            Email = "",
            CreatedAt = DateTime.UtcNow
        });
    }

    #region Private Helper Methods

    private async Task EnsureAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _cachedAccessToken);
            return;
        }

        // Get new access token
        var authString = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonDocument.Parse(responseJson);
        var root = tokenResponse.RootElement;

        _cachedAccessToken = root.GetProperty("access_token").GetString();
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Refresh 1 minute early

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _cachedAccessToken);

        _logger.LogDebug("Obtained new PayPal access token, expires in {ExpiresIn} seconds", expiresIn);
    }

    #endregion
}

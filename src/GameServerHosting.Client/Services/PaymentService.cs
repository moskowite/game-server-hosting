using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GameServerHosting.Client.Services;

/// <summary>
/// Service for handling payment operations with the API
/// </summary>
public class PaymentService
{
    private readonly HttpClient _httpClient;

    public PaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Creates a payment intent for processing payment
    /// </summary>
    public async Task<CreatePaymentIntentResponse?> CreatePaymentIntentAsync(
        long amount,
        string currency = "usd",
        Guid? orderId = null,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            var request = new CreatePaymentIntentRequest
            {
                Amount = amount,
                Currency = currency,
                OrderId = orderId,
                Metadata = metadata
            };

            var response = await _httpClient.PostAsJsonAsync("api/payments/create-intent", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error creating payment intent: {errorContent}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception creating payment intent: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Confirms a payment intent
    /// </summary>
    public async Task<ConfirmPaymentResponse?> ConfirmPaymentAsync(
        string paymentIntentId,
        string? paymentMethodId = null)
    {
        try
        {
            var request = new ConfirmPaymentRequest
            {
                PaymentIntentId = paymentIntentId,
                PaymentMethodId = paymentMethodId
            };

            var response = await _httpClient.PostAsJsonAsync("api/payments/confirm", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error confirming payment: {errorContent}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ConfirmPaymentResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception confirming payment: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets payment details by ID
    /// </summary>
    public async Task<PaymentDetailsResponse?> GetPaymentAsync(Guid paymentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/payments/{paymentId}");
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PaymentDetailsResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception getting payment: {ex.Message}");
            return null;
        }
    }
}

#region Request/Response Models

public class CreatePaymentIntentRequest
{
    public long Amount { get; set; }
    public string? Currency { get; set; }
    public Guid? OrderId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CreatePaymentIntentResponse
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ConfirmPaymentRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string? PaymentMethodId { get; set; }
}

public class ConfirmPaymentResponse
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class PaymentDetailsResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? PaymentGateway { get; set; }
    public string? PaymentIdentifier { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? RefundReason { get; set; }
}

#endregion

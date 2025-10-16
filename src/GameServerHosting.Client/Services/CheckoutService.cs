using GameServerHosting.Application.DTOs;
using GameServerHosting.Client.Models;
using GameServerHosting.Domain.Enums;
using System.Text.Json;

namespace GameServerHosting.Client.Services;

/// <summary>
/// Service for managing checkout process state and operations
/// </summary>
public class CheckoutService
{
    private readonly CartService _cartService;
    private readonly HttpClient _httpClient;
    
    private CheckoutState _state = new();
    
    public event Action<CheckoutState>? OnCheckoutStateChanged;

    public CheckoutService(CartService cartService, HttpClient httpClient)
    {
        _cartService = cartService;
        _httpClient = httpClient;
    }

    public CheckoutState State => _state;

    /// <summary>
    /// Initialize checkout with cart items
    /// </summary>
    public async Task InitializeAsync()
    {
        _state = new CheckoutState
        {
            CartItems = _cartService.CartItems.ToList(),
            CurrentStep = 1,
            StartedAt = DateTime.UtcNow
        };

        CalculatePricing();
        NotifyStateChanged();
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Move to next step in checkout process
    /// </summary>
    public void NextStep()
    {
        if (_state.CurrentStep < 5 && _state.CanProceedToNextStep())
        {
            _state.CurrentStep++;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Move to previous step in checkout process
    /// </summary>
    public void PreviousStep()
    {
        if (_state.CurrentStep > 1)
        {
            _state.CurrentStep--;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Go to a specific step
    /// </summary>
    public void GoToStep(int step)
    {
        if (step >= 1 && step <= 5)
        {
            _state.CurrentStep = step;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Update server configuration for a cart item
    /// </summary>
    public void UpdateServerConfiguration(Guid cartItemId, ServerConfiguration configuration)
    {
        _state.ServerConfigurations[cartItemId] = configuration;
        NotifyStateChanged();
    }

    /// <summary>
    /// Update billing details
    /// </summary>
    public void UpdateBillingDetails(BillingDetails billingDetails)
    {
        _state.BillingDetails = billingDetails;
        NotifyStateChanged();
    }

    /// <summary>
    /// Update payment details
    /// </summary>
    public void UpdatePaymentDetails(PaymentDetails paymentDetails)
    {
        _state.PaymentDetails = paymentDetails;
        NotifyStateChanged();
    }

    /// <summary>
    /// Apply promo code
    /// </summary>
    public async Task<bool> ApplyPromoCodeAsync(string promoCode)
    {
        // TODO: Implement API call to validate promo code
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            _state.PromoCode = promoCode;
            // Simulate discount
            _state.Discount += _state.Subtotal * 0.1m; // 10% discount
            CalculatePricing();
            NotifyStateChanged();
            return await Task.FromResult(true);
        }
        return false;
    }

    /// <summary>
    /// Remove promo code
    /// </summary>
    public void RemovePromoCode()
    {
        _state.PromoCode = null;
        CalculatePricing();
        NotifyStateChanged();
    }

    /// <summary>
    /// Process payment and complete order
    /// </summary>
    public async Task<bool> ProcessPaymentAsync()
    {
        try
        {
            // TODO: Implement actual payment processing API call
            // For now, simulate successful payment
            await Task.Delay(2000); // Simulate processing

            _state.CompletedOrder = new OrderDto
            {
                Id = Guid.NewGuid(),
                OrderNumber = GenerateOrderNumber(),
                OrderType = OrderType.NewServer,
                Status = OrderStatus.Completed,
                TotalAmount = _state.Total,
                Currency = "USD",
                PaymentMethod = _state.PaymentDetails?.PaymentMethod ?? PaymentMethod.CreditCard,
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _state.CompletedAt = DateTime.UtcNow;
            _state.CurrentStep = 5;
            
            // Clear the cart after successful order
            await _cartService.ClearCartAsync();
            
            NotifyStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Payment processing error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Reset checkout state
    /// </summary>
    public void Reset()
    {
        _state = new CheckoutState();
        NotifyStateChanged();
    }

    /// <summary>
    /// Calculate pricing including tax and discounts
    /// </summary>
    private void CalculatePricing()
    {
        _state.Subtotal = _state.CartItems.Sum(item => item.Price * item.Quantity);
        
        // Calculate discounts from cart items
        var itemDiscounts = _state.CartItems
            .Where(item => item.ServerPlan?.DiscountPercentage > 0)
            .Sum(item => item.Price * item.Quantity * (item.ServerPlan!.DiscountPercentage / 100m));
        
        _state.Discount = itemDiscounts;
        
        // Calculate tax (8% for demo purposes)
        var taxableAmount = _state.Subtotal - _state.Discount;
        _state.Tax = taxableAmount * 0.08m;
        
        _state.Total = _state.Subtotal - _state.Discount + _state.Tax;
    }

    /// <summary>
    /// Generate unique order number
    /// </summary>
    private string GenerateOrderNumber()
    {
        var date = DateTime.UtcNow;
        var random = new Random();
        return $"ORD-{date:yyyyMMdd}-{random.Next(10000, 99999)}";
    }

    private void NotifyStateChanged()
    {
        OnCheckoutStateChanged?.Invoke(_state);
    }
}

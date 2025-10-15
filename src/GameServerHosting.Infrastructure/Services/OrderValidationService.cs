using GameServerHosting.Infrastructure.Data;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Application.Interfaces;
using GameServerHosting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GameServerHosting.Infrastructure.Services;

/// <summary>
/// Service for validating orders and their associated data
/// </summary>
public class OrderValidationService : IOrderValidationService
{
    private readonly GameServerDbContext _context;
    private readonly ILogger<OrderValidationService> _logger;

    public OrderValidationService(
        GameServerDbContext context,
        ILogger<OrderValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validates cart items for an order
    /// </summary>
    public async Task<OrderValidationResult> ValidateCartItemsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var result = new OrderValidationResult();

        try
        {
            // Fetch cart items with server plan details
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .Include(ci => ci.ServerPlan)
                    .ThenInclude(sp => sp.ServerType)
                .ToListAsync(cancellationToken);

            // Check if cart is empty
            if (!cartItems.Any())
            {
                result.IsValid = false;
                result.ErrorMessage = "Cart is empty. Please add items to your cart before creating an order.";
                return result;
            }

            // Validate each cart item
            foreach (var cartItem in cartItems)
            {
                // Check if server plan exists
                if (cartItem.ServerPlan == null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Server plan not found for cart item {cartItem.Id}.";
                    return result;
                }

                // Check if server plan is active
                if (!cartItem.ServerPlan.IsActive)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Server plan '{cartItem.ServerPlan.Name}' is no longer available.";
                    return result;
                }

                // Validate quantity
                if (cartItem.Quantity <= 0)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Invalid quantity for cart item {cartItem.Id}.";
                    return result;
                }

                // Validate price is positive
                if (cartItem.Price <= 0)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Invalid price for cart item {cartItem.Id}.";
                    return result;
                }

                // Update cart item price to current server plan price if it has changed
                if (cartItem.Price != cartItem.ServerPlan.Price)
                {
                    _logger.LogInformation(
                        "Updating cart item {CartItemId} price from {OldPrice} to {NewPrice}",
                        cartItem.Id, cartItem.Price, cartItem.ServerPlan.Price);
                    
                    cartItem.Price = cartItem.ServerPlan.Price;
                    cartItem.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Save any price updates
            await _context.SaveChangesAsync(cancellationToken);

            // Calculate total amount
            decimal totalAmount = cartItems.Sum(ci => ci.Price * ci.Quantity);
            string currency = cartItems.First().Currency;

            // Verify all items use the same currency
            if (cartItems.Any(ci => ci.Currency != currency))
            {
                result.IsValid = false;
                result.ErrorMessage = "Cart contains items with different currencies. Please ensure all items use the same currency.";
                return result;
            }

            result.IsValid = true;
            result.CartItems = cartItems;
            result.TotalAmount = totalAmount;
            result.Currency = currency;

            _logger.LogInformation(
                "Cart validation successful for user {UserId}. {ItemCount} items, total: {Total} {Currency}",
                userId, cartItems.Count, totalAmount, currency);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cart items for user {UserId}", userId);
            result.IsValid = false;
            result.ErrorMessage = "An error occurred while validating cart items.";
            return result;
        }
    }

    /// <summary>
    /// Validates billing information
    /// </summary>
    public ValidationResult ValidateBillingInformation(string? billingDetails)
    {
        var result = new ValidationResult();

        try
        {
            // Check if billing details are provided
            if (string.IsNullOrWhiteSpace(billingDetails))
            {
                result.IsValid = false;
                result.ErrorMessage = "Billing information is required.";
                result.Errors.Add("Billing details cannot be empty.");
                return result;
            }

            // Try to parse as JSON
            BillingInformation? billing;
            try
            {
                billing = JsonSerializer.Deserialize<BillingInformation>(
                    billingDetails,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid billing details JSON format");
                result.IsValid = false;
                result.ErrorMessage = "Invalid billing information format.";
                result.Errors.Add("Billing details must be valid JSON.");
                return result;
            }

            if (billing == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid billing information.";
                result.Errors.Add("Billing details could not be parsed.");
                return result;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(billing.FirstName))
            {
                result.Errors.Add("First name is required.");
            }

            if (string.IsNullOrWhiteSpace(billing.LastName))
            {
                result.Errors.Add("Last name is required.");
            }

            if (string.IsNullOrWhiteSpace(billing.Email))
            {
                result.Errors.Add("Email is required.");
            }
            else if (!IsValidEmail(billing.Email))
            {
                result.Errors.Add("Email format is invalid.");
            }

            if (string.IsNullOrWhiteSpace(billing.AddressLine1))
            {
                result.Errors.Add("Address line 1 is required.");
            }

            if (string.IsNullOrWhiteSpace(billing.City))
            {
                result.Errors.Add("City is required.");
            }

            if (string.IsNullOrWhiteSpace(billing.Country))
            {
                result.Errors.Add("Country is required.");
            }

            if (string.IsNullOrWhiteSpace(billing.PostalCode))
            {
                result.Errors.Add("Postal code is required.");
            }

            // Check if there are any validation errors
            if (result.Errors.Any())
            {
                result.IsValid = false;
                result.ErrorMessage = "Billing information is incomplete or invalid.";
                _logger.LogWarning("Billing validation failed: {Errors}", string.Join(", ", result.Errors));
                return result;
            }

            result.IsValid = true;
            _logger.LogInformation("Billing information validation successful");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating billing information");
            result.IsValid = false;
            result.ErrorMessage = "An error occurred while validating billing information.";
            return result;
        }
    }

    /// <summary>
    /// Checks inventory and availability for cart items
    /// </summary>
    public async Task<ValidationResult> CheckInventoryAvailabilityAsync(
        IEnumerable<CartItem> cartItems,
        CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult();

        try
        {
            var serverPlanIds = cartItems.Select(ci => ci.ServerPlanId).Distinct().ToList();

            // Fetch all server plans to check availability
            var serverPlans = await _context.ServerPlans
                .Where(sp => serverPlanIds.Contains(sp.Id))
                .Include(sp => sp.ServerType)
                .ToListAsync(cancellationToken);

            // Check if all server plans are still available
            foreach (var cartItem in cartItems)
            {
                var serverPlan = serverPlans.FirstOrDefault(sp => sp.Id == cartItem.ServerPlanId);

                if (serverPlan == null)
                {
                    result.Errors.Add($"Server plan {cartItem.ServerPlanId} no longer exists.");
                    continue;
                }

                if (!serverPlan.IsActive)
                {
                    result.Errors.Add($"Server plan '{serverPlan.Name}' is no longer available.");
                }

                // For game server hosting, we typically don't have physical inventory limits
                // but we can check for server capacity, region availability, etc.
                // This is a placeholder for future inventory checks
                
                // Example: Check if server type is available
                if (serverPlan.ServerType != null && !serverPlan.ServerType.IsActive)
                {
                    result.Errors.Add($"Server type '{serverPlan.ServerType.GameName}' is no longer available.");
                }
            }

            if (result.Errors.Any())
            {
                result.IsValid = false;
                result.ErrorMessage = "Some items in your cart are no longer available.";
                _logger.LogWarning(
                    "Inventory availability check failed: {Errors}",
                    string.Join(", ", result.Errors));
                return result;
            }

            result.IsValid = true;
            _logger.LogInformation("Inventory availability check passed for {Count} items", cartItems.Count());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking inventory availability");
            result.IsValid = false;
            result.ErrorMessage = "An error occurred while checking inventory availability.";
            return result;
        }
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

using GameServerHosting.Application.DTOs;
using GameServerHosting.Domain.Entities;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Service for validating orders and their associated data
/// </summary>
public interface IOrderValidationService
{
    /// <summary>
    /// Validates cart items for an order
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with cart items or error message</returns>
    Task<OrderValidationResult> ValidateCartItemsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates billing information
    /// </summary>
    /// <param name="billingDetails">Billing details JSON string</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateBillingInformation(string? billingDetails);

    /// <summary>
    /// Checks inventory and availability for cart items
    /// </summary>
    /// <param name="cartItems">List of cart items</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> CheckInventoryAvailabilityAsync(IEnumerable<CartItem> cartItems, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of order validation
/// </summary>
public class OrderValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
}

/// <summary>
/// Generic validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
}

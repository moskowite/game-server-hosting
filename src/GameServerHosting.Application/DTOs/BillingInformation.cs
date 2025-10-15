using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Application.DTOs;

/// <summary>
/// Billing information for order processing
/// </summary>
public class BillingInformation
{
    /// <summary>
    /// First name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number (optional)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Address line 1
    /// </summary>
    [Required(ErrorMessage = "Address is required")]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Address line 2 (optional)
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// City
    /// </summary>
    [Required(ErrorMessage = "City is required")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State/Province (optional)
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [Required(ErrorMessage = "Postal code is required")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country
    /// </summary>
    [Required(ErrorMessage = "Country is required")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Company name (optional)
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// Tax ID/VAT number (optional)
    /// </summary>
    public string? TaxId { get; set; }
}

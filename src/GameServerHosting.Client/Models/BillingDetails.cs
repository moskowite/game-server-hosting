using System.ComponentModel.DataAnnotations;

namespace GameServerHosting.Client.Models;

/// <summary>
/// Billing information for checkout
/// </summary>
public class BillingDetails
{
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; } = string.Empty;

    public string? Address2 { get; set; }

    [Required(ErrorMessage = "City is required")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "State/Province is required")]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "ZIP/Postal code is required")]
    public string ZipCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required")]
    public string Country { get; set; } = "United States";

    public string? Phone { get; set; }

    public string? Company { get; set; }

    public string? TaxId { get; set; }
}

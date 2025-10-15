using GameServerHosting.Domain.Entities;

namespace GameServerHosting.Application.Interfaces;

/// <summary>
/// Service interface for generating invoices and receipts
/// </summary>
public interface IInvoiceService
{
    /// <summary>
    /// Generates an invoice/receipt for an order
    /// </summary>
    /// <param name="orderId">The order ID to generate invoice for</param>
    /// <returns>The invoice data as a byte array (PDF)</returns>
    Task<byte[]> GenerateInvoiceAsync(Guid orderId);

    /// <summary>
    /// Generates an invoice/receipt for multiple orders (combined invoice)
    /// </summary>
    /// <param name="orderIds">List of order IDs to include in the invoice</param>
    /// <returns>The combined invoice data as a byte array (PDF)</returns>
    Task<byte[]> GenerateCombinedInvoiceAsync(List<Guid> orderIds);

    /// <summary>
    /// Generates an HTML version of the invoice for email or web display
    /// </summary>
    /// <param name="orderId">The order ID to generate invoice for</param>
    /// <returns>HTML string of the invoice</returns>
    Task<string> GenerateInvoiceHtmlAsync(Guid orderId);
}

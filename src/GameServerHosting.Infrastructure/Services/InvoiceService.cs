using GameServerHosting.Application.Interfaces;
using GameServerHosting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GameServerHosting.Infrastructure.Services;

/// <summary>
/// Service for generating invoices and receipts
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly GameServerDbContext _context;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        GameServerDbContext context,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<byte[]> GenerateInvoiceAsync(Guid orderId)
    {
        try
        {
            var html = await GenerateInvoiceHtmlAsync(orderId);
            
            // For now, we'll return the HTML as bytes
            // In production, you'd use a PDF generation library like QuestPDF or IronPDF
            return Encoding.UTF8.GetBytes(html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for order {OrderId}", orderId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> GenerateCombinedInvoiceAsync(List<Guid> orderIds)
    {
        try
        {
            if (orderIds == null || orderIds.Count == 0)
            {
                throw new ArgumentException("At least one order ID is required", nameof(orderIds));
            }

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.ServerPlan)
                    .ThenInclude(sp => sp!.ServerType)
                .Include(o => o.Payments)
                .Where(o => orderIds.Contains(o.Id))
                .ToListAsync();

            if (orders.Count == 0)
            {
                throw new ArgumentException("No orders found with the provided IDs");
            }

            var html = GenerateCombinedInvoiceHtml(orders);
            return Encoding.UTF8.GetBytes(html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating combined invoice for orders: {OrderIds}", string.Join(", ", orderIds));
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GenerateInvoiceHtmlAsync(Guid orderId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.ServerPlan)
                    .ThenInclude(sp => sp!.ServerType)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                throw new ArgumentException($"Order {orderId} not found");
            }

            return GenerateInvoiceHtml(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice HTML for order {OrderId}", orderId);
            throw;
        }
    }

    #region Private Helper Methods

    private string GenerateInvoiceHtml(GameServerHosting.Domain.Entities.Order order)
    {
        var payment = order.Payments?.FirstOrDefault();
        var invoiceDate = order.PaymentDate ?? order.CreatedAt;
        var invoiceNumber = $"INV-{order.OrderNumber}";

        // Parse billing details from JSON
        var billingDetails = ParseBillingDetails(order.BillingDetails);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Invoice {invoiceNumber}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
        }}
        .invoice-header {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 40px;
            padding-bottom: 20px;
            border-bottom: 3px solid #667eea;
        }}
        .company-info {{
            flex: 1;
        }}
        .company-name {{
            font-size: 28px;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 5px;
        }}
        .invoice-info {{
            text-align: right;
            flex: 1;
        }}
        .invoice-title {{
            font-size: 36px;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 10px;
        }}
        .invoice-details {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 30px;
        }}
        .bill-to, .invoice-meta {{
            flex: 1;
        }}
        .section-title {{
            font-weight: bold;
            font-size: 14px;
            color: #667eea;
            margin-bottom: 10px;
            text-transform: uppercase;
        }}
        .items-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 30px 0;
        }}
        .items-table th {{
            background: #667eea;
            color: white;
            padding: 12px;
            text-align: left;
            font-weight: bold;
        }}
        .items-table th:last-child,
        .items-table td:last-child {{
            text-align: right;
        }}
        .items-table td {{
            padding: 12px;
            border-bottom: 1px solid #eee;
        }}
        .items-table tr:last-child td {{
            border-bottom: 2px solid #667eea;
        }}
        .totals {{
            margin-left: auto;
            width: 300px;
            margin-top: 20px;
        }}
        .total-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 12px;
        }}
        .total-row.grand-total {{
            background: #667eea;
            color: white;
            font-size: 18px;
            font-weight: bold;
            margin-top: 10px;
        }}
        .payment-info {{
            background: #f9f9f9;
            padding: 20px;
            border-radius: 5px;
            margin-top: 30px;
        }}
        .footer {{
            margin-top: 50px;
            padding-top: 20px;
            border-top: 1px solid #eee;
            text-align: center;
            font-size: 12px;
            color: #666;
        }}
        .status-badge {{
            display: inline-block;
            padding: 5px 15px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: bold;
            margin-left: 10px;
        }}
        .status-paid {{
            background: #4CAF50;
            color: white;
        }}
        .status-pending {{
            background: #FFC107;
            color: #333;
        }}
        @media print {{
            body {{
                padding: 0;
            }}
            .no-print {{
                display: none;
            }}
        }}
    </style>
</head>
<body>
    <div class=""invoice-header"">
        <div class=""company-info"">
            <div class=""company-name"">🎮 Game Server Hosting</div>
            <div>123 Gaming Street</div>
            <div>Server City, SC 12345</div>
            <div>United States</div>
            <div style=""margin-top: 10px;"">
                <strong>Email:</strong> billing@gameserverhosting.com<br>
                <strong>Phone:</strong> +1 (555) 123-4567
            </div>
        </div>
        <div class=""invoice-info"">
            <div class=""invoice-title"">INVOICE</div>
            <div><strong>Invoice #:</strong> {invoiceNumber}</div>
            <div><strong>Order #:</strong> {order.OrderNumber}</div>
            <div><strong>Date:</strong> {invoiceDate:MMMM dd, yyyy}</div>
            <div>
                <span class=""status-badge {(order.Status == Shared.Enums.OrderStatus.Completed ? "status-paid" : "status-pending")}"">
                    {(order.Status == Shared.Enums.OrderStatus.Completed ? "PAID" : order.Status.ToString().ToUpper())}
                </span>
            </div>
        </div>
    </div>

    <div class=""invoice-details"">
        <div class=""bill-to"">
            <div class=""section-title"">Bill To</div>
            <div><strong>{order.User.UserName}</strong></div>
            <div>{order.User.Email}</div>
            {(billingDetails != null ? $@"
            <div style=""margin-top: 10px;"">
                {billingDetails.Address}<br>
                {billingDetails.City}, {billingDetails.State} {billingDetails.ZipCode}<br>
                {billingDetails.Country}
            </div>" : "")}
        </div>
        <div class=""invoice-meta"">
            <div class=""section-title"">Payment Information</div>
            <div><strong>Payment Method:</strong> {order.PaymentMethod}</div>
            {(!string.IsNullOrEmpty(payment?.PaymentIdentifier) ? $"<div><strong>Card:</strong> {payment.PaymentIdentifier}</div>" : "")}
            {(order.PaymentDate.HasValue ? $"<div><strong>Paid On:</strong> {order.PaymentDate.Value:MMMM dd, yyyy}</div>" : "")}
            {(!string.IsNullOrEmpty(order.PaymentTransactionId) ? $"<div><strong>Transaction ID:</strong> {order.PaymentTransactionId}</div>" : "")}
        </div>
    </div>

    <table class=""items-table"">
        <thead>
            <tr>
                <th>Description</th>
                <th style=""text-align: center;"">Quantity</th>
                <th style=""text-align: right;"">Unit Price</th>
                <th style=""text-align: right;"">Amount</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>
                    <strong>{order.ServerPlan?.Name ?? "Server Plan"}</strong><br>
                    <small style=""color: #666;"">
                        {order.ServerPlan?.ServerType?.GameName ?? "Game Server"} - 
                        {order.OrderType} Order
                    </small>
                </td>
                <td style=""text-align: center;"">1</td>
                <td style=""text-align: right;"">${order.TotalAmount:F2}</td>
                <td style=""text-align: right;"">${order.TotalAmount:F2}</td>
            </tr>
        </tbody>
    </table>

    <div class=""totals"">
        <div class=""total-row"">
            <span>Subtotal:</span>
            <span>${order.TotalAmount:F2}</span>
        </div>
        <div class=""total-row"">
            <span>Tax:</span>
            <span>$0.00</span>
        </div>
        <div class=""total-row grand-total"">
            <span>Total:</span>
            <span>${order.TotalAmount:F2} {order.Currency}</span>
        </div>
    </div>

    <div class=""payment-info"">
        <div class=""section-title"">Payment Status</div>
        {(order.Status == Shared.Enums.OrderStatus.Completed ? 
            $"<p>✓ <strong>Payment Received:</strong> This invoice has been paid in full on {order.PaymentDate:MMMM dd, yyyy}.</p>" :
            "<p>⚠ <strong>Payment Pending:</strong> This invoice is awaiting payment.</p>")}
        {(payment?.CompletedAt != null ? $"<p><strong>Payment Completed At:</strong> {payment.CompletedAt.Value:MMMM dd, yyyy HH:mm:ss} UTC</p>" : "")}
    </div>

    <div class=""footer"">
        <p><strong>Thank you for your business!</strong></p>
        <p>If you have any questions about this invoice, please contact us at billing@gameserverhosting.com</p>
        <p style=""margin-top: 20px;"">&copy; {DateTime.UtcNow.Year} Game Server Hosting. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    private string GenerateCombinedInvoiceHtml(List<GameServerHosting.Domain.Entities.Order> orders)
    {
        var firstOrder = orders.First();
        var totalAmount = orders.Sum(o => o.TotalAmount);
        var invoiceDate = DateTime.UtcNow;
        var invoiceNumber = $"INV-COMBINED-{invoiceDate:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var itemsHtml = string.Join("", orders.Select(order => $@"
            <tr>
                <td>
                    <strong>{order.ServerPlan?.Name ?? "Server Plan"}</strong><br>
                    <small style=""color: #666;"">
                        Order: {order.OrderNumber} - 
                        {order.ServerPlan?.ServerType?.GameName ?? "Game Server"}
                    </small>
                </td>
                <td style=""text-align: center;"">1</td>
                <td style=""text-align: right;"">${order.TotalAmount:F2}</td>
                <td style=""text-align: right;"">${order.TotalAmount:F2}</td>
            </tr>"));

        var billingDetails = ParseBillingDetails(firstOrder.BillingDetails);

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Invoice {invoiceNumber}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
        }}
        .invoice-header {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 40px;
            padding-bottom: 20px;
            border-bottom: 3px solid #667eea;
        }}
        .company-info {{
            flex: 1;
        }}
        .company-name {{
            font-size: 28px;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 5px;
        }}
        .invoice-info {{
            text-align: right;
            flex: 1;
        }}
        .invoice-title {{
            font-size: 36px;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 10px;
        }}
        .invoice-details {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 30px;
        }}
        .bill-to, .invoice-meta {{
            flex: 1;
        }}
        .section-title {{
            font-weight: bold;
            font-size: 14px;
            color: #667eea;
            margin-bottom: 10px;
            text-transform: uppercase;
        }}
        .items-table {{
            width: 100%;
            border-collapse: collapse;
            margin: 30px 0;
        }}
        .items-table th {{
            background: #667eea;
            color: white;
            padding: 12px;
            text-align: left;
            font-weight: bold;
        }}
        .items-table th:last-child,
        .items-table td:last-child {{
            text-align: right;
        }}
        .items-table td {{
            padding: 12px;
            border-bottom: 1px solid #eee;
        }}
        .totals {{
            margin-left: auto;
            width: 300px;
            margin-top: 20px;
        }}
        .total-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 12px;
        }}
        .total-row.grand-total {{
            background: #667eea;
            color: white;
            font-size: 18px;
            font-weight: bold;
            margin-top: 10px;
        }}
        .footer {{
            margin-top: 50px;
            padding-top: 20px;
            border-top: 1px solid #eee;
            text-align: center;
            font-size: 12px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class=""invoice-header"">
        <div class=""company-info"">
            <div class=""company-name"">🎮 Game Server Hosting</div>
            <div>123 Gaming Street</div>
            <div>Server City, SC 12345</div>
            <div>United States</div>
            <div style=""margin-top: 10px;"">
                <strong>Email:</strong> billing@gameserverhosting.com<br>
                <strong>Phone:</strong> +1 (555) 123-4567
            </div>
        </div>
        <div class=""invoice-info"">
            <div class=""invoice-title"">INVOICE</div>
            <div><strong>Invoice #:</strong> {invoiceNumber}</div>
            <div><strong>Date:</strong> {invoiceDate:MMMM dd, yyyy}</div>
            <div><strong>Orders:</strong> {orders.Count}</div>
        </div>
    </div>

    <div class=""invoice-details"">
        <div class=""bill-to"">
            <div class=""section-title"">Bill To</div>
            <div><strong>{firstOrder.User.UserName}</strong></div>
            <div>{firstOrder.User.Email}</div>
            {(billingDetails != null ? $@"
            <div style=""margin-top: 10px;"">
                {billingDetails.Address}<br>
                {billingDetails.City}, {billingDetails.State} {billingDetails.ZipCode}<br>
                {billingDetails.Country}
            </div>" : "")}
        </div>
        <div class=""invoice-meta"">
            <div class=""section-title"">Order Numbers</div>
            {string.Join("<br>", orders.Select(o => o.OrderNumber))}
        </div>
    </div>

    <table class=""items-table"">
        <thead>
            <tr>
                <th>Description</th>
                <th style=""text-align: center;"">Quantity</th>
                <th style=""text-align: right;"">Unit Price</th>
                <th style=""text-align: right;"">Amount</th>
            </tr>
        </thead>
        <tbody>
            {itemsHtml}
        </tbody>
    </table>

    <div class=""totals"">
        <div class=""total-row"">
            <span>Subtotal:</span>
            <span>${totalAmount:F2}</span>
        </div>
        <div class=""total-row"">
            <span>Tax:</span>
            <span>$0.00</span>
        </div>
        <div class=""total-row grand-total"">
            <span>Total:</span>
            <span>${totalAmount:F2} {firstOrder.Currency}</span>
        </div>
    </div>

    <div class=""footer"">
        <p><strong>Thank you for your business!</strong></p>
        <p>If you have any questions about this invoice, please contact us at billing@gameserverhosting.com</p>
        <p style=""margin-top: 20px;"">&copy; {DateTime.UtcNow.Year} Game Server Hosting. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    private dynamic? ParseBillingDetails(string billingDetailsJson)
    {
        try
        {
            if (string.IsNullOrEmpty(billingDetailsJson) || billingDetailsJson == "{}")
            {
                return null;
            }

            var details = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(billingDetailsJson);
            
            return new
            {
                Address = details.TryGetProperty("address", out var addr) ? addr.GetString() : "",
                City = details.TryGetProperty("city", out var city) ? city.GetString() : "",
                State = details.TryGetProperty("state", out var state) ? state.GetString() : "",
                ZipCode = details.TryGetProperty("zipCode", out var zip) ? zip.GetString() : "",
                Country = details.TryGetProperty("country", out var country) ? country.GetString() : ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing billing details JSON: {Json}", billingDetailsJson);
            return null;
        }
    }

    #endregion
}

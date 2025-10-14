namespace GameServerHosting.Client.Models;

public enum OrderType
{
    NewServer,
    Renewal,
    Upgrade
}

public enum OrderStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public enum PaymentMethod
{
    CreditCard,
    PayPal,
    BankTransfer,
    Cryptocurrency
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string? ServerName { get; set; }
    public string? GameName { get; set; }
    public string? PlanName { get; set; }
}

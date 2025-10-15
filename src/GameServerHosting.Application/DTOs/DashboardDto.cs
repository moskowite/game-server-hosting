namespace GameServerHosting.Application.DTOs;

/// <summary>
/// DTO for dashboard overview
/// </summary>
public class DashboardDto
{
    public UserProfileResponse UserProfile { get; set; } = null!;
    public List<GameServerDto> ActiveServers { get; set; } = new();
    public List<OrderDto> RecentOrders { get; set; } = new();
    public DashboardStats Stats { get; set; } = new();
}

/// <summary>
/// Dashboard statistics
/// </summary>
public class DashboardStats
{
    public int TotalServers { get; set; }
    public int RunningServers { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public string Currency { get; set; } = "USD";
}

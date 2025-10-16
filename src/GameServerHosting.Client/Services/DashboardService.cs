using System.Net.Http.Json;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Client.Models;

namespace GameServerHosting.Client.Services;

public class DashboardService
{
    private readonly HttpClient _httpClient;

    public DashboardService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DashboardDto?> GetDashboardDataAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DashboardDto>("api/dashboard");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching dashboard data: {ex.Message}");
            return null;
        }
    }

    public async Task<List<GameServerDto>?> GetServersAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<GameServerDto>>("api/dashboard/servers");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching servers: {ex.Message}");
            return null;
        }
    }

    public async Task<List<OrderDto>?> GetOrdersAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<OrderDto>>("api/dashboard/orders");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching orders: {ex.Message}");
            return null;
        }
    }
}

using System.Net.Http.Json;
using GameServerHosting.Client.Models;

namespace GameServerHosting.Client.Services;

public class ServerPlansService
{
    private readonly HttpClient _httpClient;

    public ServerPlansService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ServerPlanDto>?> GetServerPlansByTypeAsync(Guid serverTypeId, bool? isActive = true)
    {
        try
        {
            var queryParams = new List<string>();
            if (isActive.HasValue)
            {
                queryParams.Add($"isActive={isActive.Value}");
            }

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var url = $"api/server-plans/by-type/{serverTypeId}{queryString}";

            return await _httpClient.GetFromJsonAsync<List<ServerPlanDto>>(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching server plans: {ex.Message}");
            return null;
        }
    }

    public async Task<ServerPlanDto?> GetServerPlanAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ServerPlanDto>($"api/server-plans/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching server plan: {ex.Message}");
            return null;
        }
    }
}

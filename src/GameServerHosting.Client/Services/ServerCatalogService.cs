using System.Net.Http.Json;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Client.Models;

namespace GameServerHosting.Client.Services;

public class ServerCatalogService
{
    private readonly HttpClient _httpClient;

    public ServerCatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ServerTypeDto>?> GetServerTypesAsync(bool? isActive = true, string? gameName = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (isActive.HasValue)
            {
                queryParams.Add($"isActive={isActive.Value}");
            }
            if (!string.IsNullOrWhiteSpace(gameName))
            {
                queryParams.Add($"gameName={gameName}");
            }

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var url = $"api/server-types{queryString}";

            return await _httpClient.GetFromJsonAsync<List<ServerTypeDto>>(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching server types from '{_httpClient.BaseAddress}api/server-types': {ex}");
            return null;
        }
    }

    public async Task<ServerTypeDto?> GetServerTypeAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ServerTypeDto>($"api/server-types/{id}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching server type from '{_httpClient.BaseAddress}api/server-types/{id}': {ex}");
            return null;
        }
    }
}

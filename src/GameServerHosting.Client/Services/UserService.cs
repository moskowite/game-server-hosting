using System.Net.Http.Json;
using GameServerHosting.Client.Models;

namespace GameServerHosting.Client.Services;

public class UserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserProfileResponse?> GetProfileAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserProfileResponse>("api/users/profile");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching user profile: {ex.Message}");
            return null;
        }
    }

    public async Task<UserProfileResponse?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/users/profile", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserProfileResponse>();
            }

            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error updating profile: {error}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating profile: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/users/change-password", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error changing password: {ex.Message}");
            return false;
        }
    }

    public async Task<UserPreferencesDto?> GetPreferencesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserPreferencesDto>("api/users/preferences");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching preferences: {ex.Message}");
            return null;
        }
    }

    public async Task<UserPreferencesDto?> UpdatePreferencesAsync(UserPreferencesDto preferences)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/users/preferences", preferences);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserPreferencesDto>();
            }

            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error updating preferences: {error}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating preferences: {ex.Message}");
            return null;
        }
    }
}

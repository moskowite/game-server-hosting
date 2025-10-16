using System.Net.Http.Json;
using System.Text.Json;
using GameServerHosting.Application.DTOs;
using GameServerHosting.Client.Models;
using Microsoft.JSInterop;

namespace GameServerHosting.Client.Services;

/// <summary>
/// Service for managing shopping cart state and persistence
/// </summary>
public class CartService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string CartStorageKey = "gameserver_cart";
    private List<CartItemDto> _cartItems = new();
    private bool _isInitialized = false;

    /// <summary>
    /// Event fired when cart is updated (item added, removed, quantity changed)
    /// </summary>
    public event Action<CartNotification>? OnCartUpdated;

    /// <summary>
    /// Gets the current cart items
    /// </summary>
    public IReadOnlyList<CartItemDto> CartItems => _cartItems.AsReadOnly();

    /// <summary>
    /// Gets the total number of items in the cart (sum of all quantities)
    /// </summary>
    public int ItemCount => _cartItems.Sum(item => item.Quantity);

    /// <summary>
    /// Gets the total price of all items in the cart
    /// </summary>
    public decimal TotalPrice => _cartItems.Sum(item => item.Price * item.Quantity);

    public CartService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initializes the cart service by loading cart from API or local storage
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            // Try to load from API first (if user is authenticated)
            var apiItems = await GetCartFromApiAsync();
            if (apiItems != null && apiItems.Any())
            {
                _cartItems = apiItems;
                await SaveToLocalStorageAsync();
            }
            else
            {
                // Fall back to local storage
                await LoadFromLocalStorageAsync();
            }
        }
        catch
        {
            // If API call fails (e.g., not authenticated), load from local storage
            await LoadFromLocalStorageAsync();
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    public async Task<bool> AddToCartAsync(Guid serverPlanId, int quantity = 1)
    {
        try
        {
            var request = new AddToCartRequest
            {
                ServerPlanId = serverPlanId,
                Quantity = quantity
            };

            var response = await _httpClient.PostAsJsonAsync("api/cart/add", request);
            
            if (response.IsSuccessStatusCode)
            {
                var cartItem = await response.Content.ReadFromJsonAsync<CartItemDto>();
                if (cartItem != null)
                {
                    // Update local cart
                    var existingItem = _cartItems.FirstOrDefault(i => i.ServerPlanId == serverPlanId);
                    if (existingItem != null)
                    {
                        existingItem.Quantity = cartItem.Quantity;
                        existingItem.Price = cartItem.Price;
                        existingItem.UpdatedAt = cartItem.UpdatedAt;
                    }
                    else
                    {
                        _cartItems.Add(cartItem);
                    }

                    await SaveToLocalStorageAsync();
                    NotifyCartUpdated(CartNotificationType.ItemAdded, $"Added {cartItem.ServerPlan?.Name ?? "item"} to cart");
                    return true;
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error adding to cart: {error}");
            NotifyCartUpdated(CartNotificationType.Error, "Failed to add item to cart");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding to cart: {ex.Message}");
            NotifyCartUpdated(CartNotificationType.Error, "Failed to add item to cart");
            return false;
        }
    }

    /// <summary>
    /// Updates the quantity of a cart item
    /// </summary>
    public async Task<bool> UpdateQuantityAsync(Guid itemId, int quantity)
    {
        try
        {
            var request = new UpdateCartItemRequest { Quantity = quantity };
            var response = await _httpClient.PutAsJsonAsync($"api/cart/update/{itemId}", request);
            
            if (response.IsSuccessStatusCode)
            {
                var updatedItem = await response.Content.ReadFromJsonAsync<CartItemDto>();
                if (updatedItem != null)
                {
                    var item = _cartItems.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        item.Quantity = updatedItem.Quantity;
                        item.UpdatedAt = updatedItem.UpdatedAt;
                        
                        await SaveToLocalStorageAsync();
                        NotifyCartUpdated(CartNotificationType.ItemUpdated, "Cart updated");
                        return true;
                    }
                }
            }

            NotifyCartUpdated(CartNotificationType.Error, "Failed to update item");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating cart item: {ex.Message}");
            NotifyCartUpdated(CartNotificationType.Error, "Failed to update item");
            return false;
        }
    }

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    public async Task<bool> RemoveItemAsync(Guid itemId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/cart/remove/{itemId}");
            
            if (response.IsSuccessStatusCode)
            {
                _cartItems.RemoveAll(i => i.Id == itemId);
                await SaveToLocalStorageAsync();
                NotifyCartUpdated(CartNotificationType.ItemRemoved, "Item removed from cart");
                return true;
            }

            NotifyCartUpdated(CartNotificationType.Error, "Failed to remove item");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing cart item: {ex.Message}");
            NotifyCartUpdated(CartNotificationType.Error, "Failed to remove item");
            return false;
        }
    }

    /// <summary>
    /// Clears all items from the cart
    /// </summary>
    public async Task<bool> ClearCartAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("api/cart/clear");
            
            if (response.IsSuccessStatusCode)
            {
                _cartItems.Clear();
                await SaveToLocalStorageAsync();
                NotifyCartUpdated(CartNotificationType.CartCleared, "Cart cleared");
                return true;
            }

            NotifyCartUpdated(CartNotificationType.Error, "Failed to clear cart");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing cart: {ex.Message}");
            NotifyCartUpdated(CartNotificationType.Error, "Failed to clear cart");
            return false;
        }
    }

    /// <summary>
    /// Refreshes cart from API
    /// </summary>
    public async Task RefreshAsync()
    {
        var items = await GetCartFromApiAsync();
        if (items != null)
        {
            _cartItems = items;
            await SaveToLocalStorageAsync();
            OnCartUpdated?.Invoke(new CartNotification
            {
                Type = CartNotificationType.CartRefreshed,
                Message = "Cart refreshed"
            });
        }
    }

    private async Task<List<CartItemDto>?> GetCartFromApiAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CartItemDto>>("api/cart");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching cart from API: {ex.Message}");
            return null;
        }
    }

    private async Task SaveToLocalStorageAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cartItems);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CartStorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving cart to local storage: {ex.Message}");
        }
    }

    private async Task LoadFromLocalStorageAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", CartStorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                var items = JsonSerializer.Deserialize<List<CartItemDto>>(json);
                _cartItems = items ?? new List<CartItemDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading cart from local storage: {ex.Message}");
            _cartItems = new List<CartItemDto>();
        }
    }

    private void NotifyCartUpdated(CartNotificationType type, string message)
    {
        OnCartUpdated?.Invoke(new CartNotification
        {
            Type = type,
            Message = message,
            ItemCount = ItemCount
        });
    }
}

/// <summary>
/// Notification sent when cart is updated
/// </summary>
public class CartNotification
{
    public CartNotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

/// <summary>
/// Types of cart notifications
/// </summary>
public enum CartNotificationType
{
    ItemAdded,
    ItemUpdated,
    ItemRemoved,
    CartCleared,
    CartRefreshed,
    Error
}

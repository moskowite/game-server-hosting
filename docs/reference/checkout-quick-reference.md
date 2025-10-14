# Checkout Flow - Quick Reference

## URLs
- **Checkout Page**: `/checkout`
- **Navigate From**: Cart page (`/cart`) → "Proceed to Checkout" button

## File Locations

### Models
```
src/GameServerHosting.Client/Models/
├── BillingDetails.cs
├── ServerConfiguration.cs
├── PaymentDetails.cs
└── CheckoutState.cs
```

### Service
```
src/GameServerHosting.Client/Services/
└── CheckoutService.cs
```

### Components
```
src/GameServerHosting.Client/Pages/
├── Checkout.razor (main container)
├── CheckoutStep1ReviewCart.razor
├── CheckoutStep2ServerConfig.razor
├── CheckoutStep3BillingInfo.razor
├── CheckoutStep4Payment.razor
└── CheckoutStep5Confirmation.razor
```

### Styles
```
src/GameServerHosting.Client/Pages/
├── Checkout.razor.css
└── CheckoutSteps.css
```

## How It Works

1. User clicks "Proceed to Checkout" from cart
2. `CheckoutService` initializes with cart items
3. User progresses through 5 steps:
   - Step 1: Review cart items
   - Step 2: Configure servers (name, region, settings)
   - Step 3: Enter billing info
   - Step 4: Enter payment details
   - Step 5: View confirmation

## Key Methods

### CheckoutService
```csharp
// Initialize checkout
await CheckoutService.InitializeAsync();

// Navigate steps
CheckoutService.NextStep();
CheckoutService.PreviousStep();
CheckoutService.GoToStep(stepNumber);

// Update data
CheckoutService.UpdateServerConfiguration(itemId, config);
CheckoutService.UpdateBillingDetails(billing);
CheckoutService.UpdatePaymentDetails(payment);

// Process payment
bool success = await CheckoutService.ProcessPaymentAsync();

// Subscribe to events
CheckoutService.OnCheckoutStateChanged += (state) => { };
```

## Validation

Each step validates before allowing progression:
- **Step 1**: Cart has items
- **Step 2**: All servers configured (name + region)
- **Step 3**: All billing fields complete
- **Step 4**: Payment details + terms accepted
- **Step 5**: Order completed

## Customization

### Add New Payment Method
1. Update `PaymentMethod` enum in `Models/PaymentDetails.cs`
2. Add UI in `CheckoutStep4Payment.razor`
3. Handle in `CheckoutService.ProcessPaymentAsync()`

### Change Tax Rate
Edit `CheckoutService.cs`, line ~168:
```csharp
_state.Tax = taxableAmount * 0.08m; // Change 0.08 to desired rate
```

### Add New Region
Edit `CheckoutStep2ServerConfig.razor`, add option in region select (line ~47)

### Modify Order Number Format
Edit `CheckoutService.cs`, method `GenerateOrderNumber()` (line ~184)

## Styling

All styles use CSS custom properties. To customize colors:

```css
/* In your global CSS or Checkout.razor.css */
:root {
    --bs-primary: #007bff;  /* Primary color */
    --bs-success: #28a745;  /* Success color */
}
```

## Testing Checklist

- [ ] Complete full checkout flow
- [ ] Test back navigation (step 5 → 1)
- [ ] Test validation (try to skip steps)
- [ ] Test with multiple items in cart
- [ ] Test with single item
- [ ] Test on mobile device
- [ ] Test on tablet
- [ ] Test form validation (invalid email, etc.)
- [ ] Test payment methods switching
- [ ] Test empty cart redirect

## Common Issues

### Checkout page shows empty cart
- Ensure cart has items before navigating to `/checkout`
- Check `CartService` is initialized

### Cannot proceed past a step
- Check validation requirements for that step
- Open browser console for validation errors

### Styles not loading
- Ensure CSS files are in `Pages/` directory
- Check `<link>` tags in step components

### Service not found error
- Verify `CheckoutService` is registered in `Program.cs`
- Check DI container registration

## Integration Guide

### Backend API Endpoints Needed

```http
POST /api/checkout/validate
Body: { checkoutState }
Response: { isValid, errors }

POST /api/checkout/process-payment
Body: { billingDetails, paymentDetails, cartItems }
Response: { orderId, orderNumber, status }

POST /api/orders
Body: { order details }
Response: { order }
```

### Example Integration

```csharp
// In CheckoutService.ProcessPaymentAsync()
var response = await _httpClient.PostAsJsonAsync("/api/checkout/process-payment", new
{
    BillingDetails = _state.BillingDetails,
    PaymentDetails = _state.PaymentDetails,
    CartItems = _state.CartItems,
    Total = _state.Total
});

if (response.IsSuccessStatusCode)
{
    var result = await response.Content.ReadFromJsonAsync<OrderResult>();
    _state.CompletedOrder = result.Order;
    return true;
}
```

## Support

For questions:
1. Check `CHECKOUT_FLOW_IMPLEMENTATION.md` for technical details
2. Review `docs/features/checkout-flow.md` for architecture
3. Examine individual component files for specific features

---

**Quick Start**: Navigate to `/checkout` after adding items to cart!

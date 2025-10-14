# CI Build Failures - Root Cause and Solution

## Issues Identified

The CI checks failed due to **two distinct problems** in `Settings.razor`:

---

## Issue 1: Unexpected Character Error (Line 29)

### Root Cause
```razor
@onclick="() => SetActiveTab(\"profile\")"
```

The backslash-escaped quotes (`\"`) inside the `@onclick` attribute caused a parsing error. In Blazor Razor syntax, when using lambda expressions inside attributes, escaped quotes create conflicts with the Razor parser.

### Solution
Changed from **double quotes with escaping** to **single quotes for the attribute**:

```razor
<!-- Before (ERROR) -->
@onclick="@(() => SetActiveTab(\"profile\"))"

<!-- After (FIXED) -->
@onclick='@(() => SetActiveTab("profile"))'
```

This allows double quotes to be used naturally inside the lambda expression without escaping.

**Applied to:** All 4 tab navigation buttons (profile, security, email, notifications)

---

## Issue 2: Duplicate `onchange` Attribute Error (9 instances)

### Root Cause
```razor
<input type="checkbox" 
       @bind="preferences.EmailPromotional" 
       @onchange="() => SavePreferences()">
```

Using both `@bind` and `@onchange` on the same element creates a conflict because:
- `@bind` automatically generates an `onchange` event handler for two-way data binding
- Adding explicit `@onchange` creates a duplicate attribute
- HTML/Blazor doesn't allow duplicate attributes (case-insensitive)

### Solution
Replaced `@onchange` with `@bind:after` directive:

```razor
<!-- Before (ERROR) -->
<input type="checkbox" 
       @bind="preferences.EmailPromotional" 
       @onchange="() => SavePreferences()">

<!-- After (FIXED) -->
<input type="checkbox" 
       @bind="preferences.EmailPromotional" 
       @bind:after="SavePreferences">
```

The `@bind:after` directive is the proper Blazor approach (available since .NET 6) to execute code after a binding update, without creating duplicate event handlers.

**Applied to:** All 9 preference toggles
- 4 email preferences (promotional, order updates, server alerts, newsletter)
- 5 notification preferences (browser, server offline, resource alerts, payment due, security alerts)

---

## Summary of Changes

### Files Modified
- `src/GameServerHosting.Client/Pages/Settings.razor`

### Specific Changes
1. **Tab Navigation Buttons (4 fixes):**
   - Changed attribute quotes from `"..."` to `'...'` for lambda expressions
   - Lines: 29, 36, 43, 50

2. **Preference Checkboxes (9 fixes):**
   - Replaced `@onchange="() => SavePreferences()"` with `@bind:after="SavePreferences"`
   - Lines: 261, 279, 297, 315, 357, 375, 393, 411, 429

### Verification
✅ No `@onchange` attributes remain in the file  
✅ All 9 preferences now use `@bind:after` for auto-save  
✅ All tab buttons use single-quoted attributes with unescaped inner quotes

---

## Why These Solutions Work

### Single vs Double Quotes
In Razor syntax, you can use either single or double quotes for attribute values. When the attribute value contains the same type of quote, you must either:
- Escape them (which causes parser issues in some contexts)
- Use the opposite quote type (preferred solution)

### `@bind:after` Directive
The `@bind:after` directive is specifically designed for this use case:
- Executes after the binding updates the property
- Doesn't conflict with the implicit `onchange` handler from `@bind`
- Cleaner and more maintainable than manual event handlers
- Standard Blazor pattern for "auto-save" functionality

---

## Expected CI Result
Both build failures should now be resolved:
- ✅ No more "Unexpected character '\'" errors
- ✅ No more "duplicate onchange attribute" errors
- ✅ Build should pass successfully
- ✅ Functionality preserved (auto-save still works via `@bind:after`)

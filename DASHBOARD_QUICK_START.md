# Dashboard Quick Start Guide

## 🎯 What Was Built

A complete **User Dashboard** for the Game Server Hosting platform with:
- ✅ User profile display
- ✅ Statistics cards (Total Servers, Running Servers, Total Orders, Total Spent)
- ✅ Active servers table
- ✅ Order history table
- ✅ Modern, responsive design
- ✅ RESTful API backend

## 🚀 Quick Start

### 1. Start the Backend API
```bash
cd src/GameServerHosting.Api
dotnet run
```

### 2. Start the Blazor Client
```bash
cd src/GameServerHosting.Client
dotnet run
```

### 3. Access the Dashboard
- Navigate to: `http://localhost:5000/dashboard` (or your configured port)
- Or click **"Dashboard"** in the navigation menu

## 📋 API Endpoints

All endpoints require JWT authentication:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/dashboard` | GET | Get complete dashboard data |
| `/api/dashboard/servers` | GET | Get all user's servers |
| `/api/dashboard/orders` | GET | Get all user's orders |

## 🎨 Dashboard Sections

### 1. User Profile Card
- Avatar with user initials
- Full name and email
- Email verification badge
- Beautiful gradient background

### 2. Statistics (4 Cards)
- **Total Servers** - All servers count
- **Running Servers** - Active servers count
- **Total Orders** - Completed orders count
- **Total Spent** - Sum of order amounts

### 3. Active Servers Table
Displays for each server:
- Server name and type
- Game name
- Status (Running/Stopped/Pending/Suspended/Terminated)
- IP address and port
- Resources (CPU cores, RAM MB, Max players)
- Expiration date
- Management actions

### 4. Order History Table
Displays for each order:
- Order number
- Order type (NewServer/Renewal/Upgrade)
- Server/Plan details
- Amount
- Status (Pending/Completed/Failed/Refunded)
- Payment method
- Order date

## 📁 Key Files Created

### Backend
```
Controllers/DashboardController.cs     - API endpoints
Models/DashboardDto.cs                 - Dashboard data structure
Models/GameServerDto.cs                - Server data structure
Models/OrderDto.cs                     - Order data structure
```

### Frontend
```
Pages/Dashboard.razor                  - Dashboard page
Pages/Dashboard.razor.css              - Dashboard styles
Services/DashboardService.cs           - API client service
Models/*.cs                            - Client-side DTOs
```

## 🔍 Testing Tips

### Test Scenarios

1. **Empty State**
   - New user with no servers or orders
   - Should show empty state messages

2. **With Data**
   - User with servers and orders
   - All tables should populate correctly

3. **Different Server Statuses**
   - Running (green badge)
   - Stopped (yellow badge)
   - Pending (blue badge)
   - Suspended (red badge)
   - Terminated (gray badge)

4. **Expiring Servers**
   - Servers expiring within 7 days show in red text

5. **Responsive Design**
   - Test on mobile, tablet, and desktop sizes

## 🎨 Status Colors

**Server Status:**
- 🟢 Running → Green
- 🟡 Stopped → Yellow
- 🔵 Pending → Blue
- 🔴 Suspended → Red
- ⚫ Terminated → Gray

**Order Status:**
- 🟢 Completed → Green
- 🟡 Pending → Yellow
- 🔴 Failed → Red
- 🔵 Refunded → Blue

## 🔐 Authentication

All dashboard endpoints require:
- Valid JWT token in Authorization header
- User must be authenticated
- Only shows data for the authenticated user

## 📱 Responsive Features

- **Desktop**: Full layout with all sections
- **Tablet**: Adjusted card sizes
- **Mobile**: Stacked layout, horizontal scroll on tables

## 🛠️ Customization

### Change Colors
Edit `Dashboard.razor.css`:
- Update `.stat-icon.bg-*` classes
- Modify gradient in `.user-info-card`

### Add More Statistics
Edit `DashboardStats` model:
1. Add property to `DashboardStats.cs` (API)
2. Update calculation in `DashboardController.cs`
3. Add new stat card in `Dashboard.razor`

### Modify Table Columns
Edit the `<table>` sections in `Dashboard.razor`

## 🐛 Troubleshooting

**Dashboard doesn't load:**
- Check if API is running
- Verify authentication token is valid
- Check browser console for errors

**Empty dashboard:**
- Verify user is authenticated
- Check if user has data in database
- Check API response in network tab

**Styling issues:**
- Ensure Bootstrap Icons CDN is loaded
- Check browser console for CSS errors
- Clear browser cache

## 📚 Documentation

For detailed information, see:
- `DASHBOARD_IMPLEMENTATION.md` - Complete implementation guide
- `DASHBOARD_SUMMARY.md` - Feature summary and architecture

## ✅ Checklist

Before deploying:
- [ ] Test with authenticated user
- [ ] Test empty states
- [ ] Test with real data
- [ ] Test on mobile devices
- [ ] Verify all API endpoints work
- [ ] Check performance with many servers/orders
- [ ] Test error scenarios

## 🎉 You're Done!

The dashboard is ready to use. Navigate to `/dashboard` and start exploring your game server hosting platform!

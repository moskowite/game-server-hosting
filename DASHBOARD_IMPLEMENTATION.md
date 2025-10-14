# User Dashboard Implementation

## Overview
A comprehensive user dashboard has been created for the Game Server Hosting platform using Blazor WebAssembly. The dashboard displays user information, active servers, order history, and key statistics.

## Features Implemented

### 1. **Dashboard Layout**
- Modern, responsive design with card-based layout
- Beautiful gradient user profile card
- Statistics overview with animated cards
- Mobile-responsive design

### 2. **User Information Display**
- User avatar with initials
- Full name and email
- Email verification status badge
- Account information from user profile

### 3. **Active Servers Display**
- Comprehensive server list with:
  - Server name and type
  - Game name
  - Current status (with color-coded badges)
  - IP address and port
  - Resource allocation (CPU, RAM, Max Players)
  - Expiration date (with warnings for expiring soon)
  - Action buttons for server management
- Empty state for users with no servers

### 4. **Order History**
- Recent orders table showing:
  - Order number
  - Order type (NewServer, Renewal, Upgrade)
  - Server/Plan details
  - Amount and currency
  - Payment status
  - Payment method
  - Order date
- Limited to 10 most recent orders on dashboard
- Link to view all orders
- Empty state for users with no orders

### 5. **Dashboard Statistics**
- **Total Servers**: Count of all user's servers
- **Running Servers**: Count of currently active servers
- **Total Orders**: Count of completed orders
- **Total Spent**: Sum of all completed order amounts

## Files Created

### Backend (API)

#### Controllers
- `src/GameServerHosting.Api/Controllers/DashboardController.cs`
  - `GET /api/dashboard` - Get complete dashboard data
  - `GET /api/dashboard/servers` - Get all user's servers
  - `GET /api/dashboard/orders` - Get all user's orders

#### Models/DTOs
- `src/GameServerHosting.Api/Models/DashboardDto.cs` - Dashboard data transfer object
- `src/GameServerHosting.Api/Models/GameServerDto.cs` - Game server data transfer object
- `src/GameServerHosting.Api/Models/OrderDto.cs` - Order data transfer object

### Frontend (Blazor Client)

#### Pages
- `src/GameServerHosting.Client/Pages/Dashboard.razor` - Main dashboard page
- `src/GameServerHosting.Client/Pages/Dashboard.razor.css` - Dashboard styles

#### Services
- `src/GameServerHosting.Client/Services/DashboardService.cs` - API communication service

#### Models (Client-side DTOs)
- `src/GameServerHosting.Client/Models/DashboardDto.cs`
- `src/GameServerHosting.Client/Models/GameServerDto.cs`
- `src/GameServerHosting.Client/Models/OrderDto.cs`
- `src/GameServerHosting.Client/Models/UserProfileResponse.cs`

#### Configuration Updates
- `src/GameServerHosting.Client/Program.cs` - Registered DashboardService
- `src/GameServerHosting.Client/_Imports.razor` - Added using statements for Models and Services
- `src/GameServerHosting.Client/Layout/NavMenu.razor` - Added Dashboard navigation link
- `src/GameServerHosting.Client/wwwroot/index.html` - Added Bootstrap Icons CDN

## API Endpoints

### Dashboard Controller

#### Get Dashboard Data
```http
GET /api/dashboard
Authorization: Bearer {token}
```

**Response:**
```json
{
  "userProfile": {
    "id": "guid",
    "userName": "string",
    "email": "string",
    "emailConfirmed": boolean,
    "firstName": "string",
    "lastName": "string",
    "phoneNumber": "string",
    "role": "string",
    "isActive": boolean,
    "createdAt": "datetime",
    "updatedAt": "datetime",
    "lastLoginAt": "datetime"
  },
  "activeServers": [
    {
      "id": "guid",
      "serverName": "string",
      "ipAddress": "string",
      "port": number,
      "status": "Running|Stopped|Pending|Suspended|Terminated",
      "version": "string",
      "deployedAt": "datetime",
      "expiresAt": "datetime",
      "lastHeartbeatAt": "datetime",
      "createdAt": "datetime",
      "serverTypeName": "string",
      "gameName": "string",
      "cpuCores": number,
      "ramMb": number,
      "maxPlayers": number
    }
  ],
  "recentOrders": [
    {
      "id": "guid",
      "orderNumber": "string",
      "orderType": "NewServer|Renewal|Upgrade",
      "status": "Pending|Completed|Failed|Refunded",
      "totalAmount": number,
      "currency": "string",
      "paymentMethod": "CreditCard|PayPal|BankTransfer|Cryptocurrency",
      "paymentDate": "datetime",
      "createdAt": "datetime",
      "serverName": "string",
      "gameName": "string",
      "planName": "string"
    }
  ],
  "stats": {
    "totalServers": number,
    "runningServers": number,
    "totalOrders": number,
    "totalSpent": number,
    "currency": "string"
  }
}
```

#### Get All Servers
```http
GET /api/dashboard/servers
Authorization: Bearer {token}
```

#### Get All Orders
```http
GET /api/dashboard/orders
Authorization: Bearer {token}
```

## Design Features

### Color Scheme
- **Primary**: Blue (#667eea) - Main actions, statistics
- **Success**: Green - Running servers, completed orders
- **Warning**: Orange/Yellow - Stopped servers, pending orders
- **Danger**: Red - Suspended servers, failed orders, expiring soon warnings
- **Info**: Light Blue - Pending servers, refunded orders

### Status Badges
- **Server Status**: Color-coded badges (Running=success, Stopped=warning, Pending=info, Suspended=danger, Terminated=secondary)
- **Order Status**: Color-coded badges (Completed=success, Pending=warning, Failed=danger, Refunded=info)

### Responsive Design
- Mobile-first approach
- Flexible grid layout for statistics cards
- Responsive tables with horizontal scrolling on small screens
- Stacked layout on mobile devices

### User Experience
- Loading spinner during data fetch
- Error handling with retry button
- Empty states with helpful messages
- Hover effects on cards and table rows
- Visual warnings for servers expiring soon (within 7 days)

## Navigation

The dashboard can be accessed via:
- Direct URL: `/dashboard`
- Navigation menu: "Dashboard" link

## Authentication

All dashboard endpoints require authentication via JWT Bearer token. The controller uses the `[Authorize]` attribute to ensure only authenticated users can access their data.

## Future Enhancements

Potential improvements for the dashboard:
1. Real-time server status updates using SignalR
2. Interactive charts for usage statistics
3. Server action buttons (Start, Stop, Restart, Delete)
4. Quick server creation form
5. Notification center for important alerts
6. Resource usage graphs (CPU, RAM, Disk, Network)
7. Cost breakdown and billing forecast
8. Server performance metrics
9. Bulk server actions
10. Customizable dashboard widgets

## Testing

To test the dashboard:
1. Ensure the API is running with authentication enabled
2. Log in with valid credentials
3. Navigate to `/dashboard`
4. The dashboard should display:
   - Your user information
   - Statistics cards
   - List of active servers (if any)
   - Recent order history (if any)

## Dependencies

- ASP.NET Core 8.0+
- Entity Framework Core
- ASP.NET Core Identity
- Blazor WebAssembly
- Bootstrap 5
- Bootstrap Icons (via CDN)

## Notes

- The dashboard fetches all data in a single API call for optimal performance
- Server status updates are not real-time; refresh the page to see updates
- Order history is limited to the 10 most recent orders on the main dashboard
- All monetary values are displayed in USD currency format
- Dates are displayed in UTC timezone

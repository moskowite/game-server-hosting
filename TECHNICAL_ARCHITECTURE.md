# GameHost Marketplace - Technical Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         End Users                                │
│                  (Customers & Administrators)                    │
└────────────┬────────────────────────────────────┬────────────────┘
             │                                    │
             ▼                                    ▼
┌────────────────────────┐          ┌────────────────────────────┐
│   Blazor WebAssembly   │          │      Mobile Browsers       │
│      (SPA Client)      │          │    (Responsive Design)     │
└───────────┬────────────┘          └────────────┬───────────────┘
            │                                    │
            └────────────────┬───────────────────┘
                             │ HTTPS / WSS
                             │
            ┌────────────────▼────────────────────┐
            │      Azure Application Gateway       │
            │      (SSL/TLS Termination)          │
            └────────────────┬────────────────────┘
                             │
            ┌────────────────▼────────────────────┐
            │       ASP.NET Core Web API          │
            │         (API Gateway)               │
            │  ┌────────────────────────────┐    │
            │  │   Controllers              │    │
            │  │   - AuthController         │    │
            │  │   - ServerController       │    │
            │  │   - OrderController        │    │
            │  │   - PaymentController      │    │
            │  │   - AdminController        │    │
            │  └──────────┬─────────────────┘    │
            │             │                       │
            │  ┌──────────▼─────────────────┐    │
            │  │   Services Layer           │    │
            │  │   - AuthService            │    │
            │  │   - OrderService           │    │
            │  │   - PaymentService         │    │
            │  │   - ProvisioningService    │    │
            │  │   - NotificationService    │    │
            │  └──────────┬─────────────────┘    │
            │             │                       │
            │  ┌──────────▼─────────────────┐    │
            │  │   Repository Layer         │    │
            │  │   - IRepository<T>         │    │
            │  │   - UserRepository         │    │
            │  │   - ServerRepository       │    │
            │  │   - OrderRepository        │    │
            │  └──────────┬─────────────────┘    │
            └─────────────┼─────────────────────┘
                          │
         ┌────────────────┼────────────────┐
         │                │                │
         ▼                ▼                ▼
┌─────────────┐  ┌────────────────┐  ┌──────────────┐
│ PostgreSQL  │  │  Redis Cache   │  │   Hangfire   │
│  Database   │  │  (Session &    │  │ (Background  │
│             │  │   Cache Data)  │  │    Jobs)     │
└─────────────┘  └────────────────┘  └──────┬───────┘
                                             │
                                             ▼
                                    ┌─────────────────┐
                                    │  Provisioning   │
                                    │     Queue       │
                                    └────────┬────────┘
                                             │
         ┌───────────────────────────────────┴──────────────────┐
         │                                                       │
         ▼                                                       ▼
┌──────────────────┐                                  ┌──────────────────┐
│  Infrastructure  │                                  │   Third-Party    │
│    Providers     │                                  │    Services      │
│                  │                                  │                  │
│ - AWS EC2        │                                  │ - Stripe API     │
│ - DigitalOcean   │                                  │ - SendGrid       │
│ - Azure VMs      │                                  │ - Twilio         │
└──────────────────┘                                  └──────────────────┘
```

## Architecture Layers

### 1. Presentation Layer (Frontend)

#### Blazor WebAssembly Application
- **Technology**: Blazor WebAssembly (Client-side)
- **UI Framework**: MudBlazor
- **State Management**: Fluxor (Redux pattern)
- **Routing**: Blazor Router
- **HTTP Communication**: HttpClient with Polly retry policies

**Key Components:**
```
/Pages
  - Index.razor (Landing page)
  - ServerCatalog.razor (Browse servers)
  - ServerDetails.razor (Server details)
  - Cart.razor (Shopping cart)
  - Checkout.razor (Checkout flow)
  - Dashboard.razor (User dashboard)
  - ServerControl.razor (Manage servers)
  - Admin/ (Admin pages)

/Components
  - ServerCard.razor
  - CartSummary.razor
  - PaymentForm.razor
  - NotificationBell.razor

/Services
  - ApiService.cs (API communication)
  - CartService.cs (Cart state)
  - AuthService.cs (Authentication state)
  - SignalRService.cs (Real-time updates)

/Store (Fluxor State Management)
  - CartStore/
  - AuthStore/
  - NotificationStore/
```

### 2. API Gateway Layer

#### ASP.NET Core Web API
- **Version**: .NET 8.0
- **Pattern**: RESTful API + SignalR for real-time
- **Authentication**: JWT Bearer Tokens
- **Documentation**: Swagger/OpenAPI

**Endpoints Structure:**
```
/api/auth
  POST   /register
  POST   /login
  POST   /refresh-token
  POST   /logout
  POST   /forgot-password
  POST   /reset-password

/api/users
  GET    /profile
  PUT    /profile
  PUT    /change-password
  DELETE /account

/api/server-types
  GET    /
  GET    /{id}
  POST   / (admin)
  PUT    /{id} (admin)
  DELETE /{id} (admin)

/api/server-plans
  GET    /
  GET    /{id}
  GET    /by-type/{typeId}
  POST   / (admin)
  PUT    /{id} (admin)
  DELETE /{id} (admin)

/api/servers
  GET    /user/{userId}
  GET    /{id}
  POST   /{id}/start
  POST   /{id}/stop
  POST   /{id}/restart
  DELETE /{id}

/api/cart
  GET    /
  POST   /add
  PUT    /update/{itemId}
  DELETE /remove/{itemId}
  DELETE /clear

/api/orders
  POST   /create
  GET    /{id}
  GET    /user/{userId}
  PUT    /{id}/status

/api/payments
  POST   /create-intent
  POST   /confirm
  POST   /webhook (Stripe)
  GET    /{id}

/api/billing
  GET    /history
  GET    /invoices
  GET    /upcoming

/api/admin
  GET    /users
  GET    /servers
  GET    /orders
  POST   /users/{id}/suspend
  POST   /servers/{id}/force-stop

/api/notifications
  GET    /user/{userId}
  PUT    /{id}/read
  DELETE /{id}

/api/support
  POST   /tickets
  GET    /tickets/{id}
  POST   /tickets/{id}/reply
  PUT    /tickets/{id}/status

/hubs (SignalR)
  /server-status
  /notifications
  /support-chat
```

### 3. Business Logic Layer

#### Services
- **Pattern**: Dependency Injection
- **Interface-based**: IService interfaces for testability
- **Transaction Management**: Unit of Work pattern

**Core Services:**
```csharp
// Authentication & Authorization
IAuthService
ITokenService
IUserService

// Server Management
IServerTypeService
IServerPlanService
IServerProvisioningService
IServerManagementService

// E-commerce
ICartService
IOrderService
IPaymentService
IBillingService

// Communication
IEmailService
INotificationService
ISupportService

// Background Jobs
IProvisioningJobService
IRenewalJobService
IMonitoringJobService
```

### 4. Data Access Layer

#### Entity Framework Core
- **ORM**: Entity Framework Core 8.0
- **Database**: PostgreSQL 15
- **Pattern**: Repository Pattern + Unit of Work
- **Migrations**: Code-First approach

**Domain Models:**
```csharp
// Identity
User : IdentityUser
  - Id, Email, FirstName, LastName, PhoneNumber
  - CreatedAt, LastLoginAt
  - IsActive, EmailConfirmed

Role : IdentityRole
  - Id, Name (Admin, Customer)

// Server Management
ServerType
  - Id, Name, Description, IconUrl
  - IsActive, SortOrder
  - CreatedAt, UpdatedAt

ServerPlan
  - Id, ServerTypeId, Name
  - RAM, CPU, Slots, Storage
  - PriceMonthly, PriceYearly
  - IsActive

GameServer
  - Id, UserId, ServerPlanId
  - ServerName, Status (enum)
  - IpAddress, Port
  - CreatedAt, ExpiresAt, LastStartedAt
  - ConfigurationJson

// E-commerce
Order
  - Id, UserId, OrderNumber
  - TotalAmount, Status (enum)
  - CreatedAt, CompletedAt
  - OrderItems (collection)

OrderItem
  - Id, OrderId, ServerPlanId
  - Quantity, UnitPrice, TotalPrice
  - ConfigurationJson

Payment
  - Id, OrderId, UserId
  - Amount, Currency
  - PaymentMethod, Status (enum)
  - StripePaymentIntentId
  - CreatedAt, CompletedAt

Subscription
  - Id, UserId, GameServerId
  - PlanId, Status (enum)
  - BillingCycle (enum: Monthly, Yearly)
  - CurrentPeriodStart, CurrentPeriodEnd
  - StripeSubscriptionId

// Communication
Notification
  - Id, UserId, Type (enum)
  - Title, Message
  - IsRead, CreatedAt

SupportTicket
  - Id, UserId, Subject
  - Status (enum), Priority (enum)
  - CreatedAt, UpdatedAt, ClosedAt

TicketMessage
  - Id, TicketId, UserId
  - Message, IsStaffReply
  - CreatedAt

// Audit
AuditLog
  - Id, UserId, Action
  - EntityType, EntityId
  - Changes (JSON), IpAddress
  - CreatedAt
```

**DbContext Configuration:**
```csharp
public class ApplicationDbContext : IdentityDbContext<User>
{
    public DbSet<ServerType> ServerTypes { get; set; }
    public DbSet<ServerPlan> ServerPlans { get; set; }
    public DbSet<GameServer> GameServers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<TicketMessage> TicketMessages { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Apply all configurations from separate files
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

### 5. Infrastructure Layer

#### External Services Integration

**Payment Processing (Stripe)**
```csharp
public interface IPaymentService
{
    Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency);
    Task<PaymentIntent> ConfirmPaymentAsync(string paymentIntentId);
    Task<Refund> RefundPaymentAsync(string paymentIntentId, decimal amount);
    Task<Subscription> CreateSubscriptionAsync(string customerId, string priceId);
    Task<Subscription> CancelSubscriptionAsync(string subscriptionId);
}
```

**Email Service (SendGrid)**
```csharp
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendTemplateEmailAsync(string to, string templateId, object templateData);
    Task SendOrderConfirmationAsync(Order order);
    Task SendServerProvisionedAsync(GameServer server);
    Task SendRenewalReminderAsync(Subscription subscription);
}
```

**Server Provisioning (Multi-provider)**
```csharp
public interface ICloudProviderService
{
    Task<ProvisioningResult> ProvisionServerAsync(ServerProvisionRequest request);
    Task<ServerStatus> GetServerStatusAsync(string serverId);
    Task<bool> StartServerAsync(string serverId);
    Task<bool> StopServerAsync(string serverId);
    Task<bool> RestartServerAsync(string serverId);
    Task<bool> DeleteServerAsync(string serverId);
}

// Implementations:
- DigitalOceanProviderService
- AWSProviderService
- AzureProviderService
```

### 6. Background Jobs Layer

#### Hangfire Jobs
- **Job Scheduler**: Hangfire with PostgreSQL storage
- **Job Types**: Fire-and-forget, Delayed, Recurring

**Job Definitions:**
```csharp
// Server Provisioning Job
[AutomaticRetry(Attempts = 3)]
public class ServerProvisioningJob
{
    public async Task Execute(Guid orderId)
    {
        // 1. Get order details
        // 2. Call provisioning service
        // 3. Update server status
        // 4. Send notification to user
    }
}

// Subscription Renewal Job
[RecurringJob("daily at 00:00 UTC")]
public class SubscriptionRenewalJob
{
    public async Task Execute()
    {
        // 1. Find subscriptions expiring in 7 days
        // 2. Send reminder emails
        // 3. Find expired subscriptions
        // 4. Process renewal payments
        // 5. Suspend servers if payment fails
    }
}

// Server Health Check Job
[RecurringJob("every 5 minutes")]
public class ServerHealthCheckJob
{
    public async Task Execute()
    {
        // 1. Check all active servers
        // 2. Update status
        // 3. Send alerts if down
        // 4. Log metrics
    }
}

// Cleanup Job
[RecurringJob("daily at 02:00 UTC")]
public class CleanupJob
{
    public async Task Execute()
    {
        // 1. Delete old logs
        // 2. Clean up expired sessions
        // 3. Archive old orders
        // 4. Optimize database
    }
}
```

### 7. Caching Layer

#### Redis Cache
- **Purpose**: Session data, frequently accessed data, rate limiting
- **Library**: StackExchange.Redis

**Cache Strategy:**
```csharp
public interface ICacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}

// Cached Data:
- Server type listings (1 hour)
- Server plan pricing (1 hour)
- User session data (24 hours)
- Rate limiting counters (1 minute)
- Active server status (30 seconds)
```

### 8. Real-time Communication

#### SignalR Hubs
- **Technology**: ASP.NET Core SignalR
- **Transport**: WebSockets (fallback to Server-Sent Events)

**Hub Definitions:**
```csharp
public class ServerStatusHub : Hub
{
    public async Task SubscribeToServer(Guid serverId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"server-{serverId}");
    }
    
    public async Task UnsubscribeFromServer(Guid serverId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"server-{serverId}");
    }
    
    // Server updates pushed from backend:
    // - Status changes (starting, running, stopped)
    // - Resource usage (CPU, RAM, bandwidth)
    // - Player count changes
}

public class NotificationHub : Hub
{
    public async Task SubscribeToNotifications(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }
    
    // Notifications pushed from backend:
    // - Order updates
    // - Payment confirmations
    // - Server provisioning complete
    // - Support ticket updates
}
```

## Security Architecture

### Authentication Flow

```
1. User Login
   ↓
2. Validate Credentials (Identity)
   ↓
3. Generate JWT Access Token (15 min expiry)
   ↓
4. Generate Refresh Token (7 days expiry)
   ↓
5. Return both tokens to client
   ↓
6. Client stores in memory/localStorage
   ↓
7. Include Access Token in Authorization header
   ↓
8. API validates token on each request
   ↓
9. If expired, use Refresh Token to get new Access Token
```

### Authorization Levels
```csharp
[Authorize] // Any authenticated user
[Authorize(Roles = "Customer")] // Customer only
[Authorize(Roles = "Admin")] // Admin only
[Authorize(Policy = "OwnsServer")] // Custom policy
```

### Security Measures

**API Level:**
- JWT Bearer Authentication
- Role-based Authorization
- API Rate Limiting (AspNetCoreRateLimit)
- CORS policy (whitelist origins)
- Request validation & sanitization
- SQL injection prevention (EF parameterization)
- XSS protection (automatic encoding)
- CSRF protection (anti-forgery tokens)

**Data Level:**
- Encrypted connection strings
- Password hashing (Identity default: PBKDF2)
- Sensitive data encryption at rest
- No PCI data storage (Stripe handles cards)
- Database connection pooling with limits

**Infrastructure Level:**
- HTTPS/TLS 1.3
- Security headers (HSTS, CSP, X-Frame-Options)
- DDoS protection (Azure/Cloudflare)
- Web Application Firewall (WAF)
- Regular security patches

## Data Flow Examples

### Purchase Flow
```
1. User adds server plan to cart (Frontend)
   ↓
2. POST /api/cart/add (API)
   ↓
3. CartService validates and saves to localStorage (Frontend)
   ↓
4. User proceeds to checkout (Frontend)
   ↓
5. POST /api/orders/create (API)
   ↓
6. OrderService creates order record in DB
   ↓
7. POST /api/payments/create-intent (API)
   ↓
8. PaymentService calls Stripe API
   ↓
9. Stripe returns PaymentIntent
   ↓
10. Frontend displays Stripe payment form
    ↓
11. User enters card details (handled by Stripe.js)
    ↓
12. Stripe confirms payment
    ↓
13. POST /api/payments/confirm (API)
    ↓
14. PaymentService verifies payment with Stripe
    ↓
15. Update order status to "Paid"
    ↓
16. Enqueue provisioning job (Hangfire)
    ↓
17. Return success to frontend
    ↓
18. Show confirmation page
    ↓
19. Background: ProvisioningJob executes
    ↓
20. Call cloud provider API to create server
    ↓
21. Update server status to "Provisioning"
    ↓
22. Send real-time update via SignalR
    ↓
23. Poll provider until server ready
    ↓
24. Update server status to "Running"
    ↓
25. Send email notification
    ↓
26. Send real-time update via SignalR
    ↓
27. User sees server in dashboard
```

### Server Control Flow
```
1. User clicks "Stop Server" button (Frontend)
   ↓
2. POST /api/servers/{id}/stop (API)
   ↓
3. Validate user owns server (Authorization)
   ↓
4. ServerManagementService.StopServerAsync()
   ↓
5. Update server status to "Stopping" in DB
   ↓
6. Send real-time update via SignalR
   ↓
7. Call cloud provider API to stop server
   ↓
8. Wait for confirmation
   ↓
9. Update server status to "Stopped" in DB
   ↓
10. Send real-time update via SignalR
    ↓
11. Frontend updates UI automatically
```

## Deployment Architecture

### Azure Infrastructure

```
┌─────────────────────────────────────────────────┐
│           Azure Resource Group                   │
│                                                  │
│  ┌────────────────────────────────────────┐    │
│  │   Azure Application Gateway             │    │
│  │   (Load Balancer + WAF)                 │    │
│  └───────────┬────────────────────────────┘    │
│              │                                   │
│  ┌───────────▼────────────────────────────┐    │
│  │   Azure App Service                     │    │
│  │   - API App (Linux, .NET 8)            │    │
│  │   - Auto-scaling enabled                │    │
│  │   - 2+ instances (HA)                   │    │
│  └───────────┬────────────────────────────┘    │
│              │                                   │
│  ┌───────────▼────────────────────────────┐    │
│  │   Azure Static Web Apps                 │    │
│  │   - Blazor WASM hosting                 │    │
│  │   - CDN enabled                         │    │
│  │   - Auto SSL                            │    │
│  └─────────────────────────────────────────┘   │
│                                                  │
│  ┌─────────────────────────────────────────┐   │
│  │   Azure Database for PostgreSQL         │   │
│  │   - Flexible Server                     │   │
│  │   - Auto backups                        │   │
│  │   - Read replicas (future)              │   │
│  └─────────────────────────────────────────┘   │
│                                                  │
│  ┌─────────────────────────────────────────┐   │
│  │   Azure Cache for Redis                 │   │
│  │   - Standard tier                       │   │
│  │   - 1GB cache                           │   │
│  └─────────────────────────────────────────┘   │
│                                                  │
│  ┌─────────────────────────────────────────┐   │
│  │   Azure Application Insights            │   │
│  │   - Performance monitoring              │   │
│  │   - Error tracking                      │   │
│  │   - Custom metrics                      │   │
│  └─────────────────────────────────────────┘   │
│                                                  │
│  ┌─────────────────────────────────────────┐   │
│  │   Azure Key Vault                       │   │
│  │   - API keys                            │   │
│  │   - Connection strings                  │   │
│  │   - Certificates                        │   │
│  └─────────────────────────────────────────┘   │
│                                                  │
└─────────────────────────────────────────────────┘
```

### CI/CD Pipeline (GitHub Actions)

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Test
        run: dotnet test --no-build --verbosity normal
      
      - name: Publish API
        run: dotnet publish GameHostMarketplace.API -c Release -o ./publish-api
      
      - name: Publish Web
        run: dotnet publish GameHostMarketplace.Web -c Release -o ./publish-web

  deploy-api:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v2
        with:
          app-name: gamehost-api
          publish-profile: ${{ secrets.AZURE_API_PUBLISH_PROFILE }}
          package: ./publish-api

  deploy-web:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: ./publish-web
```

## Performance Considerations

### Response Time Targets
- **API Endpoints**: < 200ms (p95)
- **Database Queries**: < 50ms (p95)
- **Cache Hits**: < 5ms
- **Page Load**: < 2 seconds (FCP)
- **Server Provisioning**: < 5 minutes

### Optimization Strategies

**Database:**
- Indexes on frequently queried columns
- Query optimization (avoid N+1)
- Connection pooling
- Read replicas for reporting

**API:**
- Response caching
- Compression (Brotli/Gzip)
- Pagination (max 100 items)
- Async/await throughout
- Minimal payload (DTOs)

**Frontend:**
- Code splitting
- Lazy loading
- Image optimization (WebP)
- CDN for static assets
- Local caching

**Caching Strategy:**
- Redis for distributed cache
- Memory cache for request-scoped data
- Output caching for public endpoints
- Cache invalidation on updates

### Scalability Plan

**Horizontal Scaling:**
- API: Auto-scale based on CPU/Memory
- Database: Read replicas
- Cache: Redis cluster
- Background Jobs: Multiple workers

**Vertical Scaling:**
- Database: Upgrade tier when needed
- Redis: Larger cache size
- App Service: Higher tier plan

## Monitoring & Observability

### Metrics to Track

**Application:**
- Request count
- Response times (p50, p95, p99)
- Error rate
- Active users
- Cache hit ratio

**Business:**
- New user registrations
- Orders completed
- Revenue per day
- Active servers
- Support tickets

**Infrastructure:**
- CPU usage
- Memory usage
- Database connections
- Queue length
- Storage usage

### Logging Strategy

**Structured Logging (Serilog):**
```csharp
Log.Information("Order created: {OrderId} by {UserId}", orderId, userId);
Log.Warning("Payment failed: {PaymentId}, Reason: {Reason}", paymentId, reason);
Log.Error(ex, "Server provisioning failed: {ServerId}", serverId);
```

**Log Levels:**
- Verbose: Detailed diagnostic info
- Debug: Debugging info
- Information: General flow
- Warning: Unexpected but handled
- Error: Errors and exceptions
- Fatal: Critical failures

**Log Destinations:**
- Console (development)
- File (structured JSON)
- Application Insights (production)
- Azure Log Analytics

### Alerting Rules

**Critical Alerts:**
- API down (5xx errors > 1%)
- Database connection failures
- Payment processing failures
- Server provisioning failures

**Warning Alerts:**
- High response times (> 1s)
- High memory usage (> 80%)
- Queue length growing
- Disk space low (< 20%)

## Disaster Recovery

### Backup Strategy

**Database:**
- Automatic daily backups (Azure)
- Point-in-time restore (7 days)
- Weekly full backups to blob storage
- Monthly snapshots (1 year retention)

**Application:**
- Source code in Git (GitHub)
- Infrastructure as Code (Terraform/ARM)
- Configuration in Key Vault
- Secrets backup

### Recovery Procedures

**Database Failure:**
1. Promote read replica (if available)
2. Restore from latest backup
3. Update connection strings
4. Verify data integrity

**Application Failure:**
1. Rollback to previous deployment
2. Check application logs
3. Fix issue
4. Redeploy

**Complete Disaster:**
1. Provision new Azure resources
2. Restore database from backup
3. Deploy latest application version
4. Update DNS records
5. Verify functionality

**RTO (Recovery Time Objective):** 4 hours  
**RPO (Recovery Point Objective):** 15 minutes

## Technology Stack Summary

### Backend
- **Runtime**: .NET 8.0
- **Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core 8.0
- **Database**: PostgreSQL 15
- **Cache**: Redis 7
- **Background Jobs**: Hangfire
- **Real-time**: SignalR
- **Authentication**: ASP.NET Core Identity + JWT
- **Logging**: Serilog
- **Testing**: xUnit, Moq, FluentAssertions

### Frontend
- **Framework**: Blazor WebAssembly
- **UI Library**: MudBlazor
- **State**: Fluxor (Redux)
- **HTTP**: HttpClient + Polly
- **Storage**: Blazored.LocalStorage

### Infrastructure
- **Cloud**: Microsoft Azure
- **CI/CD**: GitHub Actions
- **Monitoring**: Application Insights
- **Secrets**: Azure Key Vault
- **CDN**: Azure CDN / Cloudflare

### Third-Party
- **Payment**: Stripe
- **Email**: SendGrid
- **SMS**: Twilio (optional)
- **Servers**: DigitalOcean / AWS / Azure

## Cost Estimation

### Monthly Operating Costs (Estimated)

**Azure Services:**
- App Service (B1): $55/month
- Database (B1): $28/month
- Redis Cache (C1): $73/month
- Application Insights: $10-50/month
- Static Web Apps: Free tier
- Bandwidth: ~$20/month
**Subtotal: ~$186-226/month**

**Third-Party Services:**
- Stripe: 2.9% + $0.30 per transaction
- SendGrid: $15/month (40,000 emails)
- Domain: $12/year
**Subtotal: ~$16/month + transaction fees**

**Total: ~$200-250/month + transaction fees**

*Note: Costs scale with usage. Production may require higher tiers.*

## Next Steps

1. **Review Architecture**: Ensure it meets your requirements
2. **Setup Azure Account**: Create resources
3. **Start Development**: Follow roadmap Phase 1
4. **Iterate**: Build, test, deploy, repeat

---

**This architecture is designed to be:**
- ✅ Scalable (handle growth)
- ✅ Maintainable (clean code, separation of concerns)
- ✅ Secure (industry best practices)
- ✅ Performant (optimized for speed)
- ✅ Cost-effective (starts small, scales up)
- ✅ Reliable (redundancy, monitoring, backups)

Good luck building your GameHost Marketplace! 🚀


# API Development Guide

Guide for developing and extending the Game Server Hosting API.

## 🏗️ Architecture

The API follows a clean architecture pattern with clear separation of concerns:

```
Controllers/        # HTTP endpoints and request handling
  ↓
Services/          # Business logic and orchestration
  ↓
Data/              # Entity Framework Core and database access
```

## 📁 Project Structure

```
GameServerHosting.Api/
├── Controllers/              # API endpoints
│   ├── AuthController.cs
│   ├── DashboardController.cs
│   ├── ServerTypesController.cs
│   ├── ServerPlansController.cs
│   └── UsersController.cs
├── Services/                 # Business logic
│   ├── IEmailService.cs
│   ├── EmailService.cs
│   ├── IJwtTokenService.cs
│   └── JwtTokenService.cs
├── Data/                     # Database layer
│   ├── GameServerDbContext.cs
│   ├── DatabaseSeeder.cs
│   ├── Extensions/
│   └── Migrations/
├── Models/                   # DTOs and requests
│   ├── RegisterRequest.cs
│   ├── LoginRequest.cs
│   ├── AuthResponse.cs
│   └── ... more models
├── appsettings.json         # Configuration
├── appsettings.Development.json
└── Program.cs               # App startup

```

## 🔧 Configuration

### Development Settings

**File**: `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gameserverhosting_dev;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "DevSecretKey_ThisIsOnlyForDevelopment_MustBeAtLeast32Characters!",
    "ExpirationInMinutes": 120
  },
  "EmailSettings": {
    "Provider": "SMTP",
    "FromEmail": "dev@gameserverhosting.local",
    "Smtp": {
      "Host": "localhost",
      "Port": 1025,
      "EnableSsl": false
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5106",
      "http://localhost:5173"
    ]
  },
  "Swagger": {
    "Enabled": true
  }
}
```

## 🛣️ Creating a New Endpoint

### Step 1: Define the Model/DTO

```csharp
// Models/CreateServerRequest.cs
public class CreateServerRequest
{
    [Required]
    [StringLength(100)]
    public string ServerName { get; set; } = string.Empty;
    
    [Required]
    public Guid ServerTypeId { get; set; }
}

// Models/ServerResponse.cs
public class ServerResponse
{
    public Guid Id { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
}
```

### Step 2: Create the Service (Optional)

```csharp
// Services/IServerService.cs
public interface IServerService
{
    Task<ServerResponse> CreateServerAsync(CreateServerRequest request, string userId);
    Task<ServerResponse?> GetServerAsync(Guid id);
    Task<List<ServerResponse>> GetUserServersAsync(string userId);
}

// Services/ServerService.cs
public class ServerService : IServerService
{
    private readonly GameServerDbContext _context;
    
    public ServerService(GameServerDbContext context)
    {
        _context = context;
    }
    
    public async Task<ServerResponse> CreateServerAsync(CreateServerRequest request, string userId)
    {
        var server = new GameServer
        {
            Id = Guid.NewGuid(),
            ServerName = request.ServerName,
            ServerTypeId = request.ServerTypeId,
            UserId = userId,
            Status = ServerStatus.Provisioning,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.GameServers.Add(server);
        await _context.SaveChangesAsync();
        
        return new ServerResponse
        {
            Id = server.Id,
            ServerName = server.ServerName,
            Status = server.Status.ToString(),
            IpAddress = server.IpAddress ?? "",
            Port = server.Port
        };
    }
}
```

### Step 3: Register the Service

```csharp
// Program.cs
builder.Services.AddScoped<IServerService, ServerService>();
```

### Step 4: Create the Controller

```csharp
// Controllers/ServersController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServersController : ControllerBase
{
    private readonly IServerService _serverService;
    
    public ServersController(IServerService serverService)
    {
        _serverService = serverService;
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(ServerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServerResponse>> CreateServer([FromBody] CreateServerRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
            
        var server = await _serverService.CreateServerAsync(request, userId);
        return CreatedAtAction(nameof(GetServer), new { id = server.Id }, server);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServerResponse>> GetServer(Guid id)
    {
        var server = await _serverService.GetServerAsync(id);
        if (server == null)
            return NotFound();
            
        return Ok(server);
    }
}
```

## 🔐 Authentication & Authorization

### JWT Configuration

**Settings**: `appsettings.json`
```json
{
  "JwtSettings": {
    "SecretKey": "YOUR_SECRET_KEY_AT_LEAST_32_CHARACTERS",
    "ExpirationInMinutes": 60,
    "Issuer": "GameServerHosting",
    "Audience": "GameServerHostingClient"
  }
}
```

### Using Authorization

```csharp
[Authorize] // Any authenticated user
public class DashboardController : ControllerBase { }

[Authorize(Roles = "Admin")] // Admin only
public class AdminController : ControllerBase { }

[AllowAnonymous] // Public endpoint
[HttpGet("health")]
public IActionResult Health() => Ok("Healthy");
```

### Getting Current User

```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
```

## 📧 Email Service

### Configuration

See `EmailSettings` in `appsettings.json`.

### Usage

```csharp
// Inject the service
private readonly IEmailService _emailService;

// Send email
await _emailService.SendEmailAsync(
    to: user.Email,
    subject: "Welcome!",
    body: "Welcome to Game Server Hosting!"
);

// Send template email
await _emailService.SendVerificationEmailAsync(user, verificationToken);
```

## 🗃️ Database Access

### Using DbContext

```csharp
// Inject DbContext
private readonly GameServerDbContext _context;

// Query data
var servers = await _context.GameServers
    .Where(s => s.UserId == userId)
    .Include(s => s.ServerType)
    .ToListAsync();

// Add entity
_context.GameServers.Add(server);
await _context.SaveChangesAsync();

// Update entity
server.Status = ServerStatus.Running;
_context.GameServers.Update(server);
await _context.SaveChangesAsync();

// Delete entity
_context.GameServers.Remove(server);
await _context.SaveChangesAsync();
```

### Querying Best Practices

```csharp
// ✅ Use AsNoTracking for read-only queries
var servers = await _context.GameServers
    .AsNoTracking()
    .ToListAsync();

// ✅ Use projection to select only needed fields
var serverNames = await _context.GameServers
    .Select(s => new { s.Id, s.ServerName })
    .ToListAsync();

// ✅ Use Include for related data
var servers = await _context.GameServers
    .Include(s => s.ServerType)
    .Include(s => s.User)
    .ToListAsync();

// ❌ Avoid N+1 queries
foreach (var server in servers)
{
    var type = await _context.ServerTypes.FindAsync(server.ServerTypeId); // BAD!
}
```

## 📝 Logging

### Using Serilog

```csharp
// Inject ILogger
private readonly ILogger<MyController> _logger;

// Log messages
_logger.LogInformation("Server created: {ServerId}", serverId);
_logger.LogWarning("Server provisioning delayed: {ServerId}", serverId);
_logger.LogError(ex, "Failed to create server: {ServerId}", serverId);

// Structured logging
_logger.LogInformation(
    "Order completed: {OrderId} for User: {UserId}, Amount: {Amount}",
    orderId, userId, amount
);
```

## 🧪 Testing

### Testing Controllers

```csharp
public class ServersControllerTests
{
    private readonly Mock<IServerService> _mockServerService;
    private readonly ServersController _controller;
    
    public ServersControllerTests()
    {
        _mockServerService = new Mock<IServerService>();
        _controller = new ServersController(_mockServerService.Object);
    }
    
    [Fact]
    public async Task CreateServer_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateServerRequest { ServerName = "Test", ServerTypeId = Guid.NewGuid() };
        var response = new ServerResponse { Id = Guid.NewGuid(), ServerName = "Test" };
        
        _mockServerService
            .Setup(s => s.CreateServerAsync(request, It.IsAny<string>()))
            .ReturnsAsync(response);
        
        // Act
        var result = await _controller.CreateServer(request);
        
        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult.Value.Should().BeEquivalentTo(response);
    }
}
```

## 🚀 API Endpoints

### Current Endpoints

**Authentication:**
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh-token` - Refresh JWT token
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password
- `POST /api/auth/verify-email` - Verify email address
- `POST /api/auth/resend-verification` - Resend verification email

**Dashboard:**
- `GET /api/dashboard` - Get dashboard overview
- `GET /api/dashboard/servers` - Get user's servers
- `GET /api/dashboard/orders` - Get user's orders

**Server Types:**
- `GET /api/server-types` - List all server types
- `GET /api/server-types/{id}` - Get server type details
- `POST /api/server-types` - Create server type (admin)
- `PUT /api/server-types/{id}` - Update server type (admin)
- `DELETE /api/server-types/{id}` - Delete server type (admin)

**Server Plans:**
- `GET /api/server-plans` - List all plans
- `GET /api/server-plans/{id}` - Get plan details
- `POST /api/server-plans` - Create plan (admin)
- `PUT /api/server-plans/{id}` - Update plan (admin)
- `DELETE /api/server-plans/{id}` - Delete plan (admin)

**Users:**
- `GET /api/users/profile` - Get user profile
- `PUT /api/users/profile` - Update profile
- `PUT /api/users/change-password` - Change password

## 🛡️ Security Best Practices

1. **Always validate input** with Data Annotations
2. **Use parameterized queries** (EF does this automatically)
3. **Never log sensitive data** (passwords, tokens, credit cards)
4. **Always use HTTPS** in production
5. **Implement rate limiting** for public endpoints
6. **Validate user permissions** before operations
7. **Sanitize error messages** before sending to client

## 📚 Related Documentation

- [Authentication](../features/authentication.md) - JWT and Identity setup
- [Database Schema](database-schema.md) - Complete schema reference
- [Client Development](client-development.md) - Frontend integration
- [Troubleshooting](../reference/troubleshooting.md) - Common issues

---

**Happy API Development!** 🚀


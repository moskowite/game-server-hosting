# Domain Events & Background Processing Implementation

## Overview

This document describes the implementation of domain events and asynchronous background processing using MassTransit and RabbitMQ for the Game Server Hosting platform.

## Architecture

### Event-Driven Flow

```
User Request → Controller → Create Entities → Enqueue Events (Outbox) → Commit Transaction → Return 202 Accepted
                                                        ↓
                                              Outbox Dispatcher (Background Service)
                                                        ↓
                                              Publish to RabbitMQ (MassTransit)
                                                        ↓
                                              Consumers Process Events
                                                        ↓
                                      Update Domain State → Publish New Events
```

### Key Components

1. **Event Contracts** (`GameServerHosting.Shared/Events/`)
   - `BaseEvent` - Base class with correlation/causation tracking
   - `ServerProvisioningRequested` - Initiate server provisioning
   - `ServerProvisioned` - Server successfully provisioned
   - `ServerProvisioningFailed` - Server provisioning failed
   - `PaymentAuthorized` - Payment authorized
   - `PaymentCaptured` - Payment successfully captured
   - `PaymentFailed` - Payment failed
   - `OrderCreated` - Order created
   - `ServerHealthCheckFailed` - Server health check failed

2. **Outbox Pattern** (Transactional Event Publishing)
   - `OutboxMessage` entity - Stores events before publishing
   - `OutboxService` - Enqueues events within transactions
   - `OutboxDispatcher` - Background service that publishes pending events

3. **Consumers** (`GameServerHosting.Infrastructure/Consumers/`)
   - `ServerProvisioningRequestedConsumer` - Provisions servers via cloud provider
   - `PaymentAuthorizedConsumer` - Captures authorized payments
   - `NotificationConsumer` - Sends email notifications for lifecycle events

4. **Async Operations Tracking**
   - `AsyncOperation` entity - Tracks long-running operations
   - `OperationsController` - API for checking operation status

## Configuration

### RabbitMQ Setup

#### Docker Compose
```yaml
rabbitmq:
  image: rabbitmq:3.13-management-alpine
  ports:
    - "5672:5672"   # AMQP
    - "15672:15672" # Management UI
  environment:
    RABBITMQ_DEFAULT_USER: gameserver
    RABBITMQ_DEFAULT_PASS: gameserver_dev_password
    RABBITMQ_DEFAULT_VHOST: gameserver
```

#### appsettings.json
```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "gameserver",
    "Username": "gameserver",
    "Password": "gameserver_dev_password"
  }
}
```

### MassTransit Configuration

The system is configured with:
- **Exponential retry**: 5 attempts with backoff (1s → 5min)
- **Per-queue configuration**:
  - `server-provisioning-queue`: 5 retries with longer intervals
  - `payment-queue`: 3 retries with shorter intervals
  - `notification-queue`: No retries (best-effort)
- **Circuit breakers** for external service calls

## Usage

### Creating an Async Order

**Request:**
```http
POST /api/orders-async/create
Content-Type: application/json
Authorization: Bearer <token>

{
  "serverPlanId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "paymentMethod": "CreditCard",
  "billingDetails": "{}",
  "serverName": "my-game-server",
  "region": "nyc3"
}
```

**Response (202 Accepted):**
```json
{
  "operationId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "correlationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "orderId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "orderNumber": "ORD-20251015-00001",
  "gameServerId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "Pending",
  "message": "Order created successfully. Server provisioning has been initiated.",
  "statusUrl": "/api/operations/9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"
}
```

### Checking Operation Status

**Request:**
```http
GET /api/operations/{operationId}
Authorization: Bearer <token>
```

**Response:**
```json
{
  "id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
  "operationType": "ServerProvisioning",
  "status": "InProgress",
  "resourceId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "resourceType": "GameServer",
  "progressPercentage": 25,
  "statusMessage": "Provisioning server with cloud provider...",
  "createdAt": "2025-10-15T10:30:00Z",
  "updatedAt": "2025-10-15T10:30:15Z"
}
```

## Event Flow Examples

### Server Provisioning Flow

1. **User creates order** → `POST /api/orders-async/create`
2. **Controller creates entities**:
   - Order (Pending)
   - GameServer (Pending)
   - AsyncOperation (Pending, 0%)
3. **Controller enqueues events**:
   - `OrderCreated`
   - `ServerProvisioningRequested`
4. **Transaction commits** → Events saved to outbox
5. **OutboxDispatcher publishes** events to RabbitMQ
6. **ServerProvisioningRequestedConsumer**:
   - Updates AsyncOperation (InProgress, 25%)
   - Calls cloud provider API
   - Updates GameServer (Running) with IP/Port
   - Updates AsyncOperation (Succeeded, 100%)
   - Enqueues `ServerProvisioned` event
7. **NotificationConsumer**:
   - Sends success email to user

### Failure Handling

If provisioning fails:
1. Consumer catches exception
2. GameServer status → Failed
3. AsyncOperation status → Failed with error
4. `ServerProvisioningFailed` event published
5. NotificationConsumer sends failure email
6. Retry logic (if retryable):
   - Exponential backoff: 5s, 15s, 30s, 1m, 5m
   - After max retries → Dead Letter Queue

## Database Schema

### New Tables

#### outbox_messages
```sql
CREATE TABLE outbox_messages (
  id UUID PRIMARY KEY,
  event_type VARCHAR(500) NOT NULL,
  payload JSONB NOT NULL,
  created_at TIMESTAMP NOT NULL,
  processed_at TIMESTAMP,
  error TEXT,
  processing_attempts INTEGER DEFAULT 0,
  correlation_id UUID
);

CREATE INDEX idx_outbox_messages_pending ON outbox_messages(created_at) 
  WHERE processed_at IS NULL;
```

#### async_operations
```sql
CREATE TABLE async_operations (
  id UUID PRIMARY KEY,
  user_id UUID NOT NULL,
  operation_type VARCHAR(100) NOT NULL,
  status VARCHAR(20) NOT NULL,
  resource_id UUID,
  resource_type VARCHAR(100),
  correlation_id UUID NOT NULL UNIQUE,
  progress_percentage INTEGER,
  status_message VARCHAR(500),
  error_message TEXT,
  error_code VARCHAR(50),
  created_at TIMESTAMP NOT NULL,
  updated_at TIMESTAMP NOT NULL,
  completed_at TIMESTAMP,
  metadata JSONB
);
```

#### idempotency_keys
```sql
CREATE TABLE idempotency_keys (
  key VARCHAR(255) PRIMARY KEY,
  user_id UUID NOT NULL,
  request_type VARCHAR(100) NOT NULL,
  resource_id UUID,
  status_code INTEGER NOT NULL,
  response_body JSONB,
  created_at TIMESTAMP NOT NULL,
  expires_at TIMESTAMP
);
```

## Observability

### Correlation ID Flow

Every operation has a `CorrelationId` that flows through:
1. Initial HTTP request
2. Domain entities
3. All events
4. All log statements
5. AsyncOperation tracking

Example log output:
```
[2025-10-15 10:30:00.123] [INF] [AsyncOrdersController] Created order ORD-20251015-00001 for user abc123 with correlation 7c9e6679-7425-40de-944b-e07fc1f90ae7
[2025-10-15 10:30:05.456] [INF] [OutboxDispatcher] Published outbox message def456 of type ServerProvisioningRequested
[2025-10-15 10:30:10.789] [INF] [ServerProvisioningRequestedConsumer] Processing ServerProvisioningRequested for GameServer ghi789, Correlation 7c9e6679-7425-40de-944b-e07fc1f90ae7
```

### Metrics (To Be Implemented)

- Event publish count by type
- Consumer processing duration
- Retry count per event type
- Dead letter queue depth
- Outbox processing lag

## Idempotency (To Be Implemented)

### Design

Clients can provide an `X-Idempotency-Key` header:
```http
POST /api/orders-async/create
X-Idempotency-Key: unique-key-123
Authorization: Bearer <token>
```

The system:
1. Checks if key exists for this user + request type
2. If exists: Returns cached response
3. If new: Processes request and caches response
4. Keys expire after 24 hours

### Implementation Notes

Add middleware to intercept requests with idempotency keys:
```csharp
app.UseMiddleware<IdempotencyMiddleware>();
```

## Testing

### Unit Tests (To Be Implemented)

- `OutboxService` enqueuing and marking messages
- `OutboxDispatcher` processing pending messages
- Consumer logic with mocked dependencies
- Event serialization/deserialization

### Integration Tests (To Be Implemented)

Use MassTransit In-Memory Test Harness:
```csharp
[Fact]
public async Task ServerProvisioningRequested_ShouldProvisionServer()
{
    var harness = new InMemoryTestHarness();
    var consumerHarness = harness.Consumer<ServerProvisioningRequestedConsumer>();

    await harness.Start();

    await harness.InputQueueSendEndpoint.Send(new ServerProvisioningRequested
    {
        GameServerId = Guid.NewGuid(),
        // ...
    });

    Assert.True(await harness.Consumed.Any<ServerProvisioningRequested>());
    Assert.True(await consumerHarness.Consumed.Any<ServerProvisioningRequested>());

    await harness.Stop();
}
```

## Local Development

### Starting the Stack

```bash
# Start all services
docker-compose up -d

# View RabbitMQ Management UI
open http://localhost:15672
# Username: gameserver
# Password: gameserver_dev_password

# View logs
docker-compose logs -f api

# Stop all services
docker-compose down
```

### Monitoring

- **RabbitMQ Management UI**: http://localhost:15672
  - View queues, messages, consumers
  - Monitor throughput and latency
  - Inspect dead letter queues

- **Application Logs**: Structured with Serilog
  - Console output with correlation IDs
  - File output in `logs/` directory

## Migration from Sync to Async

### Before (Synchronous)
```csharp
[HttpPost("create")]
public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request)
{
    // Create order
    var order = new Order { /* ... */ };
    _context.Orders.Add(order);
    
    // Provision server (blocks request)
    var server = await _provisioningService.ProvisionServerAsync(order.Id);
    
    // Process payment (blocks request)
    await _paymentService.ProcessPaymentAsync(order.Id);
    
    await _context.SaveChangesAsync();
    return Ok(orderDto); // 200 OK after everything completes
}
```

### After (Asynchronous)
```csharp
[HttpPost("create")]
public async Task<ActionResult<AsyncOrderCreationResponse>> CreateOrderAsync([FromBody] CreateAsyncOrderRequest request)
{
    var correlationId = Guid.NewGuid();
    
    // Create order and operation tracking
    var order = new Order { /* ... */ };
    var operation = new AsyncOperation { CorrelationId = correlationId };
    
    // Enqueue events (doesn't block)
    await _outboxService.EnqueueAsync(new ServerProvisioningRequested { /* ... */ });
    
    await _unitOfWork.CommitTransactionAsync();
    
    return Accepted(new AsyncOrderCreationResponse
    {
        OperationId = operation.Id,
        StatusUrl = $"/api/operations/{operation.Id}"
    }); // 202 Accepted immediately
}
```

## Benefits

1. **Responsiveness**: Controllers return immediately (202 Accepted)
2. **Reliability**: Outbox pattern ensures events aren't lost
3. **Scalability**: Background workers can be scaled independently
4. **Resilience**: Automatic retries with exponential backoff
5. **Observability**: Correlation IDs and structured logging
6. **Testability**: Consumers can be tested in isolation
7. **Flexibility**: Easy to add new event types and consumers

## Future Enhancements

- [ ] Implement idempotency middleware
- [ ] Add metrics/telemetry (Prometheus, Grafana)
- [ ] Implement saga pattern for complex workflows
- [ ] Add SignalR for real-time status updates
- [ ] Multi-region RabbitMQ federation
- [ ] Event versioning strategy
- [ ] Circuit breaker for external services
- [ ] Comprehensive test suite

## Troubleshooting

### Events Not Being Processed

1. Check RabbitMQ is running: `docker ps | grep rabbitmq`
2. Check OutboxDispatcher is running: Check application logs
3. Check outbox table: `SELECT * FROM outbox_messages WHERE processed_at IS NULL;`
4. Check RabbitMQ queues: http://localhost:15672/#/queues

### Consumer Errors

1. Check RabbitMQ dead letter queue
2. Check application logs for stack traces
3. Verify environment variables for external services (Stripe, DigitalOcean)

### Slow Processing

1. Check RabbitMQ consumer count
2. Increase consumer prefetch count in MassTransit config
3. Scale out consumer instances (horizontal scaling)

## References

- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Management](https://www.rabbitmq.com/management.html)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Domain Events](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

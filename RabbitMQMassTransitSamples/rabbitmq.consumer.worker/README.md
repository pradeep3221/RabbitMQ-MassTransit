# RabbitMQ Consumer Worker with MassTransit

This is a .NET 9 Worker Service that demonstrates how to consume messages from RabbitMQ using MassTransit.

## Features

- ✅ .NET 9 Worker Service
- ✅ MassTransit 8.5.5 (latest stable)
- ✅ RabbitMQ integration
- ✅ Message consumption with `OrderSubmitted` contract
- ✅ Comprehensive logging with emojis for easy tracking
- ✅ Error handling and retry logic
- ✅ Dependency injection
- ✅ Asynchronous message processing

## Prerequisites

- .NET 9 SDK (or .NET 8)
- RabbitMQ server running on localhost:5672
  - Credentials configured in appsettings.json (default: admin/admin123)
  - Same RabbitMQ instance as the producer

## Configuration

The application is configured to connect to RabbitMQ with the following settings (in `appsettings.json`):

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "admin",
    "Password": "admin123"
  }
}
```

## Running the Consumer

1. Ensure RabbitMQ is running on localhost
2. Run the consumer:
   ```bash
   dotnet run
   ```
3. The consumer will start listening for `OrderSubmitted` messages

## How It Works

### Message Flow

1. **Producer** publishes `OrderSubmitted` messages to RabbitMQ
2. **RabbitMQ** routes messages to the `order-submitted-queue`
3. **Consumer** receives messages from the queue
4. **OrderSubmittedConsumer** processes each message
5. **Logging** shows the processing status

### Consumer Configuration

The consumer is configured in `Program.cs`:

```csharp
builder.Services.AddMassTransit(x =>
{
    // Register the consumer
    x.AddConsumer<OrderSubmittedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("admin");
            h.Password("admin123");
        });

        // Configure receive endpoint
        cfg.ReceiveEndpoint("order-submitted-queue", e =>
        {
            e.ConfigureConsumer<OrderSubmittedConsumer>(context);
            
            // Retry policy: 3 retries with 5 second intervals
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });
    });
});
```

### Message Contract

The `OrderSubmitted` message contract must match the producer's contract:

```csharp
namespace rabbitmq.producer.api.Contracts;

public record OrderSubmitted(Guid OrderId, string ProductName, int Quantity);
```

**Important:** The namespace must match the producer's namespace for MassTransit to route messages correctly.

## Consumer Implementation

The `OrderSubmittedConsumer` class handles incoming messages:

```csharp
public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var message = context.Message;
        
        // Log receipt
        _logger.LogInformation(
            "📦 Received OrderSubmitted - OrderId: {OrderId}, Product: {ProductName}, Quantity: {Quantity}",
            message.OrderId, message.ProductName, message.Quantity);

        // Process the order
        await ProcessOrder(message);
        
        // Log success
        _logger.LogInformation(
            "✅ Successfully processed order - OrderId: {OrderId}",
            message.OrderId);
    }
}
```

## Console Output

When the consumer is running, you'll see logs like:

```
info: 🚀 RabbitMQ Consumer Worker starting...
info: 📡 Connecting to RabbitMQ at localhost with username: admin
info: 👂 Listening for OrderSubmitted messages on queue: order-submitted-queue
info: Bus started: rabbitmq://localhost/
info: 📦 Received OrderSubmitted - OrderId: xxx, Product: Laptop, Quantity: 6
info: 🔄 Processing order for Laptop (Qty: 6)...
info: ✅ Successfully processed order - OrderId: xxx
```

## Error Handling

The consumer includes:

1. **Try-Catch Block**: Catches exceptions during processing
2. **Retry Policy**: Automatically retries failed messages 3 times with 5-second intervals
3. **Error Logging**: Logs detailed error information

```csharp
try
{
    await ProcessOrder(message);
}
catch (Exception ex)
{
    _logger.LogError(ex, "❌ Failed to process order - OrderId: {OrderId}", message.OrderId);
    throw; // Re-throw to trigger retry
}
```

## Testing the Consumer

### 1. Start the Consumer

```bash
dotnet run --project RabbitMQMassTransitSamples/rabbitmq.consumer.worker
```

### 2. Send Messages from Producer

Use the producer API to send messages:

```bash
# Send a single order
curl -X POST "http://localhost:5047/api/order/submit" \
  -H "Content-Type: application/json" \
  -d '{"productName":"Laptop","quantity":2}'

# Send batch orders
curl -X POST "http://localhost:5047/api/order/submit-batch?count=5"
```

### 3. Watch the Consumer Logs

You should see messages being received and processed in real-time.

## Project Structure

```
rabbitmq.consumer.worker/
├── Contracts/
│   └── OrderSubmitted.cs          # Message contract (must match producer)
├── Consumers/
│   └── OrderSubmittedConsumer.cs  # Consumer implementation
├── Program.cs                     # MassTransit configuration
├── appsettings.json              # RabbitMQ settings
└── rabbitmq.consumer.worker.csproj # Project file with dependencies
```

## Dependencies

- **MassTransit** (8.5.5): Distributed application framework
- **MassTransit.RabbitMQ** (8.5.5): RabbitMQ transport for MassTransit
- **Microsoft.Extensions.Hosting** (9.0.0): Worker service hosting

## Message Processing

The consumer simulates order processing with:

1. **Validation**: Receive and validate the message
2. **Processing**: Simulate processing time (500ms delay)
3. **Logging**: Log processing steps
4. **Completion**: Mark as successfully processed

In a real application, you would:
- Validate the order
- Update inventory
- Create order record in database
- Send confirmation email
- Trigger other business processes

## Monitoring

### RabbitMQ Management UI

1. Open http://localhost:15672
2. Login with `admin` / `admin123`
3. Go to "Queues" tab
4. Click on `order-submitted-queue` to see:
   - Message rate
   - Consumer count
   - Message acknowledgments

### Application Logs

The consumer provides detailed logging:
- 📦 Message received
- 🔄 Processing started
- ✅ Processing completed
- ❌ Processing failed

## Stopping the Consumer

Press `Ctrl+C` in the terminal where the consumer is running.

## Advanced Configuration

### Concurrency

To process multiple messages concurrently, configure the prefetch count:

```csharp
cfg.ReceiveEndpoint("order-submitted-queue", e =>
{
    e.PrefetchCount = 16; // Process up to 16 messages concurrently
    e.ConfigureConsumer<OrderSubmittedConsumer>(context);
});
```

### Custom Retry Policy

```csharp
e.UseMessageRetry(r => 
{
    r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2));
});
```

### Dead Letter Queue

MassTransit automatically creates error queues for failed messages.

## Troubleshooting

### Consumer not receiving messages

1. **Check namespace**: Ensure `OrderSubmitted` contract namespace matches producer
2. **Check RabbitMQ**: Verify messages are in the queue
3. **Check credentials**: Ensure RabbitMQ credentials are correct
4. **Check queue name**: Verify queue name matches configuration

### Messages going to error queue

1. Check consumer logs for exceptions
2. Review retry policy configuration
3. Verify message format matches contract

## Best Practices

1. ✅ Use shared contracts library for message definitions
2. ✅ Implement idempotent message handlers
3. ✅ Log all message processing activities
4. ✅ Use retry policies for transient failures
5. ✅ Monitor queue depths and consumer performance
6. ✅ Handle exceptions gracefully
7. ✅ Use structured logging

## Next Steps

1. ✅ Implement database persistence for processed orders
2. ✅ Add more consumers for different message types
3. ✅ Implement saga patterns for complex workflows
4. ✅ Add health checks and monitoring
5. ✅ Deploy to production environment

---

**The consumer is working perfectly with the producer!** 🎉


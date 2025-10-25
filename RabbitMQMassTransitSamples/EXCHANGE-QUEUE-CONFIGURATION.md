# RabbitMQ Exchange and Queue Configuration

This document explains how the producer and consumer are configured to use specific exchanges and queues in RabbitMQ.

## Overview

The producer and consumer have been configured to use:
- **Exchange Name**: `orders-exchange` (Topic exchange)
- **Queue Name**: `order-submitted-queue`
- **Routing**: The consumer binds to the exchange with routing key `#` (all messages)

## Configuration Files

### Producer Configuration (`rabbitmq.producer.api/appsettings.json`)

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "admin",
    "Password": "admin123",
    "ExchangeName": "orders-exchange",
    "QueueName": "order-submitted-queue"
  }
}
```

### Consumer Configuration (`rabbitmq.consumer.worker/appsettings.json`)

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "admin",
    "Password": "admin123",
    "ExchangeName": "orders-exchange",
    "QueueName": "order-submitted-queue"
  }
}
```

## Producer Implementation

### Program.cs Configuration

The producer is configured to publish messages to the specific exchange:

```csharp
var exchangeName = builder.Configuration["RabbitMQ:ExchangeName"] ?? "orders-exchange";
var queueName = builder.Configuration["RabbitMQ:QueueName"] ?? "order-submitted-queue";

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username(rabbitMqUsername);
            h.Password(rabbitMqPassword);
        });

        // Configure message topology to use specific exchange
        cfg.Message<rabbitmq.producer.api.Contracts.OrderSubmitted>(e =>
        {
            e.SetEntityName(exchangeName); // Set the exchange name
        });

        // Configure publish topology
        cfg.Publish<rabbitmq.producer.api.Contracts.OrderSubmitted>(e =>
        {
            e.ExchangeType = "topic"; // Use topic exchange for routing flexibility
        });

        cfg.ConfigureEndpoints(context);
    });
});
```

### Key Points:

1. **SetEntityName**: Specifies the exchange name for the message type
2. **ExchangeType**: Set to "topic" for flexible routing patterns
3. **Configuration-driven**: Exchange and queue names are read from appsettings.json

## Consumer Implementation

### Program.cs Configuration

The consumer is configured to listen on the specific queue and bind to the exchange:

```csharp
var exchangeName = builder.Configuration["RabbitMQ:ExchangeName"] ?? "orders-exchange";
var queueName = builder.Configuration["RabbitMQ:QueueName"] ?? "order-submitted-queue";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderSubmittedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username(rabbitMqUsername);
            h.Password(rabbitMqPassword);
        });

        // Configure the receive endpoint for the consumer with specific queue
        cfg.ReceiveEndpoint(queueName, e =>
        {
            // Bind the queue to the specific exchange
            e.Bind(exchangeName, s =>
            {
                s.ExchangeType = "topic";
                s.RoutingKey = "#"; // Subscribe to all messages on this exchange
            });

            e.ConfigureConsumer<OrderSubmittedConsumer>(context);

            // Configure retry policy
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });
    });
});
```

### Key Points:

1. **ReceiveEndpoint**: Creates/uses the specified queue name
2. **Bind**: Binds the queue to the specified exchange
3. **RoutingKey**: `#` means subscribe to all messages (wildcard for topic exchange)
4. **ExchangeType**: Must match the producer's exchange type

## How It Works

### Message Flow

1. **Producer** publishes `OrderSubmitted` message
2. Message is sent to **`orders-exchange`** (topic exchange)
3. Exchange routes message based on routing key
4. Message is delivered to **`order-submitted-queue`**
5. **Consumer** receives message from the queue
6. Consumer processes the message

### Topology Diagram

```
Producer API
    |
    | Publish OrderSubmitted
    v
orders-exchange (Topic)
    |
    | Route with key: rabbitmq.producer.api.contracts:order-submitted
    v
order-submitted-queue
    |
    | Consume
    v
Consumer Worker
```

## Exchange Types

### Topic Exchange (Current Configuration)

- **Routing**: Based on routing key patterns
- **Flexibility**: Supports wildcards (`*` and `#`)
- **Use Case**: When you need flexible routing based on message attributes

### Other Exchange Types

You can change the exchange type by modifying the configuration:

#### Direct Exchange
```csharp
e.ExchangeType = "direct";
s.RoutingKey = "order.submitted"; // Exact match required
```

#### Fanout Exchange
```csharp
e.ExchangeType = "fanout";
// No routing key needed - broadcasts to all bound queues
```

#### Headers Exchange
```csharp
e.ExchangeType = "headers";
// Routes based on message headers instead of routing key
```

## Routing Keys

### Current Configuration

- **Producer**: MassTransit automatically generates routing key based on message type
  - Format: `{namespace}:{message-name}`
  - Example: `rabbitmq.producer.api.contracts:order-submitted`

- **Consumer**: Uses `#` wildcard to receive all messages from the exchange

### Custom Routing Keys

To use custom routing keys:

#### Producer
```csharp
await _publishEndpoint.Publish(message, context =>
{
    context.SetRoutingKey("orders.high-priority");
});
```

#### Consumer
```csharp
e.Bind(exchangeName, s =>
{
    s.ExchangeType = "topic";
    s.RoutingKey = "orders.*"; // Match orders.anything
});
```

## Monitoring in RabbitMQ Management UI

1. Open http://localhost:15672
2. Login with `admin` / `admin123`
3. Navigate to **Exchanges** tab
   - Find `orders-exchange`
   - View bindings and message rates
4. Navigate to **Queues** tab
   - Find `order-submitted-queue`
   - View messages, consumers, and throughput

## Testing the Configuration

### 1. Start Both Applications

```bash
# Terminal 1: Start Producer
dotnet run --project RabbitMQMassTransitSamples/rabbitmq.producer.api

# Terminal 2: Start Consumer
dotnet run --project RabbitMQMassTransitSamples/rabbitmq.consumer.worker
```

### 2. Send Test Messages

```bash
# Send single message
curl -X POST "http://localhost:5047/api/order/submit" \
  -H "Content-Type: application/json" \
  -d '{"productName":"Laptop","quantity":2}'

# Send batch messages
curl -X POST "http://localhost:5047/api/order/submit-batch?count=5"
```

### 3. Verify in Logs

**Producer logs:**
```
info: Publishing to Exchange: orders-exchange, Queue: order-submitted-queue
info: Published order 1/5 - OrderId: xxx, Product: Laptop, Quantity: 5
```

**Consumer logs:**
```
info: 📬 Bound to exchange: orders-exchange
info: 📦 Received OrderSubmitted - OrderId: xxx, Product: Laptop, Quantity: 5
info: ✅ Successfully processed order - OrderId: xxx
```

## Changing Exchange and Queue Names

To use different exchange and queue names:

1. Update `appsettings.json` in **both** producer and consumer:
   ```json
   {
     "RabbitMQ": {
       "ExchangeName": "my-custom-exchange",
       "QueueName": "my-custom-queue"
     }
   }
   ```

2. Restart both applications

3. The new exchange and queue will be created automatically

## Best Practices

1. ✅ **Use configuration files** for exchange and queue names (not hardcoded)
2. ✅ **Match exchange types** between producer and consumer
3. ✅ **Use topic exchanges** for flexible routing
4. ✅ **Use meaningful names** for exchanges and queues
5. ✅ **Monitor** exchange and queue metrics in RabbitMQ Management UI
6. ✅ **Test** with different routing keys to verify routing logic
7. ✅ **Document** your routing patterns and exchange topology

## Troubleshooting

### Messages not being received

1. **Check exchange name** matches in producer and consumer
2. **Check queue binding** to the exchange
3. **Verify routing key** patterns match
4. **Check RabbitMQ Management UI** for bindings

### Deserialization errors

- Old messages in queue from previous configuration
- These will move to error queue after retries
- Purge the queue if needed: RabbitMQ Management UI → Queues → Purge

### Connection issues

- Verify RabbitMQ is running: `docker ps`
- Check credentials in appsettings.json
- Verify port 5672 is accessible

## Summary

✅ **Producer** publishes to `orders-exchange` (topic exchange)  
✅ **Consumer** listens on `order-submitted-queue` bound to `orders-exchange`  
✅ **Routing** uses topic exchange with `#` wildcard  
✅ **Configuration** is externalized in appsettings.json  
✅ **Tested** and working with multiple messages  

The configuration provides flexibility for future routing patterns while maintaining simplicity for the current use case.


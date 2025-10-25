# RabbitMQ Producer API with MassTransit

This is a .NET 8 Web API that demonstrates how to publish messages to RabbitMQ using MassTransit.

## Features

- ✅ .NET 8 Web API
- ✅ MassTransit 8.5.5 (latest stable)
- ✅ RabbitMQ integration
- ✅ Message publishing with `OrderSubmitted` contract
- ✅ Comprehensive logging
- ✅ Swagger/OpenAPI documentation
- ✅ Dependency injection
- ✅ Single and batch message publishing

## Prerequisites

- .NET 8 SDK
- RabbitMQ server running on localhost:5672
  - Credentials configured in appsettings.json (default: admin/admin123)
  - You can run RabbitMQ using Docker:
    ```bash
    docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=admin -e RABBITMQ_DEFAULT_PASS=admin123 rabbitmq:3-management
    ```

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

## Running the Application

1. Ensure RabbitMQ is running on localhost
2. Run the application:
   ```bash
   dotnet run
   ```
3. The API will start on `https://localhost:7xxx` (check console output for exact port)
4. Navigate to `https://localhost:7xxx/swagger` to access the Swagger UI

## API Endpoints

### 1. Submit Single Order

**POST** `/api/order/submit`

Publishes a single `OrderSubmitted` message to RabbitMQ.

**Request Body:**
```json
{
  "productName": "Laptop",
  "quantity": 2
}
```

**Response:**
```json
{
  "message": "Order submitted successfully",
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "productName": "Laptop",
  "quantity": 2,
  "status": "Published to RabbitMQ"
}
```

### 2. Submit Batch Orders

**POST** `/api/order/submit-batch?count=5`

Publishes multiple test orders to RabbitMQ.

**Query Parameters:**
- `count` (optional): Number of orders to publish (1-100, default: 5)

**Response:**
```json
{
  "message": "Batch orders submitted",
  "totalRequested": 5,
  "successfullyPublished": 5,
  "orders": [
    {
      "orderId": "guid-1",
      "productName": "Laptop",
      "quantity": 3
    },
    // ... more orders
  ]
}
```

## Message Contract

The `OrderSubmitted` message contract is defined as:

```csharp
public record OrderSubmitted(Guid OrderId, string ProductName, int Quantity);
```

## Logging

The application provides detailed console logging for:
- MassTransit connection status
- RabbitMQ connection establishment
- Message publishing events
- Success/failure confirmations
- Error details

Example log output:
```
info: rabbitmq.producer.api.Program[0]
      MassTransit configured with RabbitMQ at localhost
info: rabbitmq.producer.api.Program[0]
      RabbitMQ connection established with username: guest
info: rabbitmq.producer.api.Controllers.OrderController[0]
      Publishing OrderSubmitted message - OrderId: 3fa85f64-5717-4562-b3fc-2c963f66afa6, Product: Laptop, Quantity: 2
info: rabbitmq.producer.api.Controllers.OrderController[0]
      Successfully published OrderSubmitted message - OrderId: 3fa85f64-5717-4562-b3fc-2c963f66afa6
```

## Testing with cURL

### Submit a single order:
```bash
curl -X POST "https://localhost:7xxx/api/order/submit" \
  -H "Content-Type: application/json" \
  -d '{"productName":"Laptop","quantity":2}'
```

### Submit batch orders:
```bash
curl -X POST "https://localhost:7xxx/api/order/submit-batch?count=10"
```

## Monitoring RabbitMQ

You can monitor the messages in RabbitMQ Management UI:
1. Navigate to `http://localhost:15672`
2. Login with username: `admin`, password: `admin123`
3. Go to the "Queues" tab to see the published messages

## Project Structure

```
rabbitmq.producer.api/
├── Contracts/
│   └── OrderSubmitted.cs          # Message contract
├── Controllers/
│   └── OrderController.cs         # API controller for publishing messages
├── Program.cs                     # Application entry point with MassTransit configuration
├── appsettings.json              # Configuration including RabbitMQ settings
└── rabbitmq.producer.api.csproj  # Project file with dependencies
```

## Dependencies

- **MassTransit** (8.5.5): Distributed application framework
- **MassTransit.RabbitMQ** (8.5.5): RabbitMQ transport for MassTransit
- **Swashbuckle.AspNetCore** (6.6.2): Swagger/OpenAPI support

## Notes

- This is a **producer-only** application - it does not include any consumer/receiver logic
- The bus is automatically started and stopped by the ASP.NET Core host
- Messages are published using `IPublishEndpoint` which is injected via dependency injection
- All operations are asynchronous for better performance


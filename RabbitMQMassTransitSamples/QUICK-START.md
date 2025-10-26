# Quick Start Guide - RabbitMQ Producer with MassTransit

## ✅ Your Application is Running!

The RabbitMQ producer application is now successfully running and connected to RabbitMQ.

### 🔗 Important URLs

- **Swagger UI**: http://localhost:5047/swagger
- **RabbitMQ Management**: http://localhost:15672
  - Username: `guest`
  - Password: `guest`


Note: ADD both applicaton to same docker network rabbitmq-net
```powershell
docker network create rabbitmq-net
docker network inspect rabbitmq-net
  docker network connect rabbitmq-net rabbitmq-producer-api
docker run -d --name rabbitmq-producer-api --network rabbitmq-net -p 5047:8080 rabbitmq-producer-api

docker run --hostname=rabbitmq --env=RABBITMQ_DEFAULT_USER=admin --env=RABBITMQ_DEFAULT_PASS=admin123 --env=RABBITMQ_ERLANG_COOKIE=unique-cookie-value --env=PATH=/opt/rabbitmq/sbin:/opt/erlang/bin:/opt/openssl/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin --env=ERLANG_INSTALL_PATH_PREFIX=/opt/erlang --env=OPENSSL_INSTALL_PATH_PREFIX=/opt/openssl --env=RABBITMQ_DATA_DIR=/var/lib/rabbitmq --env=RABBITMQ_VERSION=3.13.7 --env=RABBITMQ_PGP_KEY_ID=0x0A9AF2115F4687BD29803A206B73A36E6026DFCA --env=RABBITMQ_HOME=/opt/rabbitmq --env=HOME=/var/lib/rabbitmq --env=LANG=C.UTF-8 --env=LANGUAGE=C.UTF-8 --env=LC_ALL=C.UTF-8 --volume=rabbitmq_data:/var/lib/rabbitmq --volume=/var/lib/rabbitmq --network=rabbitmq-net -p 15672:15672 -p 15692:15692 -p 5671:5671 -p 5672:5672 --restart=no --label='org.opencontainers.image.ref.name=ubuntu' --label='org.opencontainers.image.version=24.04' --runtime=runc -d rabbitmq:3.13-management
```

### 📊 What Just Happened?

We successfully published 3 test messages to RabbitMQ:

```
✅ Order 1: Laptop (Quantity: 5)
✅ Order 2: Mouse (Quantity: 1)
✅ Order 3: Keyboard (Quantity: 4)
```

### 🧪 Test the Application

#### Option 1: Using Swagger UI (Easiest)

1. Open http://localhost:5047/swagger in your browser
2. Expand the `/api/order/submit` endpoint
3. Click "Try it out"
4. Enter test data:
   ```json
   {
     "productName": "Gaming Monitor",
     "quantity": 1
   }
   ```
5. Click "Execute"
6. Check the response and logs

#### Option 2: Using PowerShell

**Submit a single order:**
```powershell
Invoke-RestMethod -Uri 'http://localhost:5047/api/order/submit' `
  -Method POST `
  -ContentType 'application/json' `
  -Body '{"productName":"Gaming Mouse","quantity":3}' | ConvertTo-Json
```

**Submit batch orders:**
```powershell
Invoke-RestMethod -Uri 'http://localhost:5047/api/order/submit-batch?count=5' `
  -Method POST | ConvertTo-Json -Depth 5
```

#### Option 3: Using cURL

**Submit a single order:**
```bash
curl -X POST "http://localhost:5047/api/order/submit" \
  -H "Content-Type: application/json" \
  -d '{"productName":"Mechanical Keyboard","quantity":2}'
```

**Submit batch orders:**
```bash
curl -X POST "http://localhost:5047/api/order/submit-batch?count=10"
```

### 📈 Monitor Messages in RabbitMQ

1. Open http://localhost:15672
2. Login with:
   - Username: `admin`
   - Password: `admin123`
3. Click on the "Queues" tab
4. You should see a queue named something like: `rabbitmq.producer.api.Contracts:OrderSubmitted`
5. Click on the queue name to see message details
6. Click "Get messages" to view the message content

### 📝 Application Logs

Watch the console where the application is running. You'll see logs like:

```
info: Starting batch publish of 3 orders
info: Published order 1/3 - OrderId: xxx, Product: Laptop, Quantity: 5
info: Published order 2/3 - OrderId: xxx, Product: Mouse, Quantity: 1
info: Published order 3/3 - OrderId: xxx, Product: Keyboard, Quantity: 4
info: Batch publish completed - 3/3 orders published successfully
```

### 🎯 API Endpoints

#### 1. Submit Single Order
- **URL**: `POST /api/order/submit`
- **Body**:
  ```json
  {
    "productName": "string",
    "quantity": number
  }
  ```
- **Response**:
  ```json
  {
    "message": "Order submitted successfully",
    "orderId": "guid",
    "productName": "string",
    "quantity": number,
    "status": "Published to RabbitMQ"
  }
  ```

#### 2. Submit Batch Orders
- **URL**: `POST /api/order/submit-batch?count={number}`
- **Query Params**:
  - `count`: Number of orders (1-100, default: 5)
- **Response**:
  ```json
  {
    "message": "Batch orders submitted",
    "totalRequested": number,
    "successfullyPublished": number,
    "orders": [...]
  }
  ```

### 🔧 Configuration

The application is configured in `appsettings.json`:

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "admin",
    "Password": "admin123"
  }
}
```

### 🛑 Stopping the Application

Press `Ctrl+C` in the terminal where the application is running.

### 🔄 Restarting the Application

```bash
dotnet run --project RabbitMQMassTransitSamples/rabbitmq.producer.api
```

### 📚 Project Structure

```
rabbitmq.producer.api/
├── Contracts/
│   └── OrderSubmitted.cs          # Message contract
├── Controllers/
│   └── OrderController.cs         # API endpoints
├── Program.cs                     # MassTransit configuration
├── appsettings.json              # RabbitMQ settings
└── rabbitmq.producer.api.csproj  # Dependencies
```

### 🎓 Key Features Implemented

✅ .NET 8 Web API  
✅ MassTransit 8.5.5 with RabbitMQ  
✅ Message publishing with `OrderSubmitted` contract  
✅ Comprehensive logging  
✅ Dependency injection  
✅ Swagger/OpenAPI documentation  
✅ Single and batch message publishing  
✅ Error handling  

### 🐛 Troubleshooting

**If you see connection errors:**
1. Make sure RabbitMQ is running: `docker ps | Select-String rabbitmq`
2. Check credentials in `appsettings.json` match your RabbitMQ setup
3. Verify RabbitMQ is accessible at http://localhost:15672

**If messages aren't appearing in RabbitMQ:**
1. Check the application logs for errors
2. Verify the queue was created in RabbitMQ Management UI
3. Make sure you're looking at the correct queue (it will have the message type name)

### 🎉 Next Steps

1. ✅ Test both API endpoints
2. ✅ Monitor messages in RabbitMQ Management UI
3. ✅ Experiment with different message payloads
4. ✅ Consider creating a consumer application to process the messages

### 📖 Additional Documentation

- See `README.md` for detailed documentation
- See `SETUP-GUIDE.md` for setup instructions
- See `test-requests.http` for HTTP test examples

---

**Congratulations!** Your RabbitMQ producer is working perfectly! 🎊


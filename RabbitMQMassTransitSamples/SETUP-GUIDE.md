# RabbitMQ + MassTransit Setup Guide

This guide will help you set up and run the RabbitMQ producer application.

## Prerequisites

- ✅ .NET 8 SDK installed
- ✅ Docker Desktop installed and running (for RabbitMQ)

## Quick Start

### Step 1: Start RabbitMQ

**Option A: Using PowerShell Script (Easiest)**

```powershell
# From the RabbitMQMassTransitSamples directory
.\start-rabbitmq.ps1
```

**Option B: Using Docker Compose**

```bash
# From the RabbitMQMassTransitSamples directory
docker-compose up -d
```

**Option C: Using Docker Command Directly**

```bash
docker run -d --name rabbitmq-masstransit -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

### Step 2: Verify RabbitMQ is Running

1. Open your browser and navigate to: **http://localhost:15672**
2. Login with:
   - Username: `guest`
   - Password: `guest`
3. You should see the RabbitMQ Management Dashboard

### Step 3: Run the Producer Application

```bash
# From the RabbitMQMassTransitSamples directory
cd rabbitmq.producer.api
dotnet run
```

Or from the root:

```bash
dotnet run --project RabbitMQMassTransitSamples/rabbitmq.producer.api
```

### Step 4: Test the Application

**Option A: Using Swagger UI**

1. Open your browser to: `https://localhost:7xxx/swagger` (check console for exact port)
2. Try the `/api/order/submit` endpoint
3. Click "Try it out"
4. Enter sample data:
   ```json
   {
     "productName": "Laptop",
     "quantity": 2
   }
   ```
5. Click "Execute"

**Option B: Using the HTTP Test File**

If you're using Visual Studio or VS Code with REST Client extension:
1. Open `test-requests.http`
2. Click "Send Request" above any request

**Option C: Using PowerShell**

```powershell
# Submit a single order
Invoke-RestMethod -Uri "https://localhost:7001/api/order/submit" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"productName":"Laptop","quantity":2}' `
  -SkipCertificateCheck

# Submit batch orders
Invoke-RestMethod -Uri "https://localhost:7001/api/order/submit-batch?count=5" `
  -Method POST `
  -SkipCertificateCheck
```

### Step 5: Monitor Messages in RabbitMQ

1. Go to RabbitMQ Management UI: **http://localhost:15672**
2. Click on "Queues" tab
3. You should see queues created by MassTransit
4. Click on a queue to see the messages

## Troubleshooting

### Error: "Connection refused" or "BrokerUnreachableException"

**Problem:** RabbitMQ is not running.

**Solution:**
1. Make sure Docker Desktop is running
2. Run the start script: `.\start-rabbitmq.ps1`
3. Wait 10-20 seconds for RabbitMQ to fully start
4. Verify it's running: `docker ps | Select-String rabbitmq`

### Error: "Port 5672 is already in use"

**Problem:** Another instance of RabbitMQ or another service is using port 5672.

**Solution:**
1. Stop the existing RabbitMQ: `.\stop-rabbitmq.ps1`
2. Or find and stop the conflicting service:
   ```powershell
   Get-NetTCPConnection -LocalPort 5672
   ```

### Error: "Docker is not running"

**Problem:** Docker Desktop is not started.

**Solution:**
1. Start Docker Desktop
2. Wait for it to fully start (check system tray icon)
3. Run the start script again

### RabbitMQ Management UI not loading

**Problem:** RabbitMQ is still starting up.

**Solution:**
1. Wait 10-20 seconds
2. Check container logs:
   ```bash
   docker logs rabbitmq-masstransit
   ```
3. Look for "Server startup complete" message

## Stopping RabbitMQ

When you're done testing:

```powershell
# Using the stop script
.\stop-rabbitmq.ps1

# Or using Docker directly
docker stop rabbitmq-masstransit

# To remove the container completely
docker rm rabbitmq-masstransit
```

## Useful Docker Commands

```bash
# Check if RabbitMQ is running
docker ps | Select-String rabbitmq

# View RabbitMQ logs
docker logs rabbitmq-masstransit

# Follow logs in real-time
docker logs -f rabbitmq-masstransit

# Restart RabbitMQ
docker restart rabbitmq-masstransit

# Stop RabbitMQ
docker stop rabbitmq-masstransit

# Start existing RabbitMQ container
docker start rabbitmq-masstransit

# Remove RabbitMQ container
docker rm rabbitmq-masstransit
```

## Expected Console Output

When the producer application runs successfully, you should see:

```
info: rabbitmq.producer.api.Program[0]
      MassTransit configured with RabbitMQ at localhost
info: rabbitmq.producer.api.Program[0]
      RabbitMQ connection established with username: guest
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

When you publish a message:

```
info: rabbitmq.producer.api.Controllers.OrderController[0]
      Publishing OrderSubmitted message - OrderId: 3fa85f64-5717-4562-b3fc-2c963f66afa6, Product: Laptop, Quantity: 2
info: rabbitmq.producer.api.Controllers.OrderController[0]
      Successfully published OrderSubmitted message - OrderId: 3fa85f64-5717-4562-b3fc-2c963f66afa6
```

## Next Steps

Once everything is working:

1. ✅ Experiment with different message payloads
2. ✅ Try the batch endpoint to publish multiple messages
3. ✅ Monitor the messages in RabbitMQ Management UI
4. ✅ Consider creating a consumer application to process the messages

## Additional Resources

- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Docker Documentation](https://docs.docker.com/)


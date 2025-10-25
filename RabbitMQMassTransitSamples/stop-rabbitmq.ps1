# PowerShell script to stop RabbitMQ Docker container

Write-Host "Stopping RabbitMQ container..." -ForegroundColor Yellow

docker stop rabbitmq-masstransit

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ RabbitMQ stopped successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to stop RabbitMQ or container not found" -ForegroundColor Red
}


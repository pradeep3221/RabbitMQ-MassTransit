# PowerShell script to start RabbitMQ using Docker

Write-Host "Starting RabbitMQ with Docker..." -ForegroundColor Green

# Check if Docker is running
try {
    docker info | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Docker is not installed or not running. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if RabbitMQ container already exists
$existingContainer = docker ps -a --filter "name=rabbitmq-masstransit" --format "{{.Names}}"

if ($existingContainer) {
    Write-Host "RabbitMQ container already exists. Starting it..." -ForegroundColor Yellow
    docker start rabbitmq-masstransit
} else {
    Write-Host "Creating and starting new RabbitMQ container..." -ForegroundColor Yellow
    docker run -d `
        --name rabbitmq-masstransit `
        -p 5672:5672 `
        -p 15672:15672 `
        -e RABBITMQ_DEFAULT_USER=guest `
        -e RABBITMQ_DEFAULT_PASS=guest `
        rabbitmq:3-management
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ RabbitMQ started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 RabbitMQ Management UI: http://localhost:15672" -ForegroundColor Cyan
    Write-Host "   Username: guest" -ForegroundColor Cyan
    Write-Host "   Password: guest" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "🔌 AMQP Connection: localhost:5672" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "⏳ Waiting for RabbitMQ to be ready (this may take 10-20 seconds)..." -ForegroundColor Yellow
    
    # Wait for RabbitMQ to be ready
    $maxAttempts = 30
    $attempt = 0
    $ready = $false
    
    while ($attempt -lt $maxAttempts -and -not $ready) {
        Start-Sleep -Seconds 2
        $attempt++
        
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:15672" -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                $ready = $true
            }
        } catch {
            Write-Host "." -NoNewline
        }
    }
    
    Write-Host ""
    if ($ready) {
        Write-Host "✅ RabbitMQ is ready!" -ForegroundColor Green
        Write-Host ""
        Write-Host "You can now run your producer application:" -ForegroundColor White
        Write-Host "  dotnet run --project rabbitmq.producer.api" -ForegroundColor Cyan
    } else {
        Write-Host "⚠️  RabbitMQ is starting but not ready yet. Please wait a few more seconds." -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Failed to start RabbitMQ" -ForegroundColor Red
    exit 1
}


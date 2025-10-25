using MassTransit;
using rabbitmq.producer.api.Contracts;

namespace rabbitmq.producer.worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly Random _random = new();
    private readonly string[] _products = new[] 
    { 
        "Laptop", "Smartphone", "Headphones", "Mouse", "Keyboard",
        "Monitor", "Printer", "Camera", "Tablet", "Speaker" 
    };

    public Worker(ILogger<Worker> logger, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = new OrderSubmitted(
                    OrderId: Guid.NewGuid(),
                    ProductName: _products[_random.Next(_products.Length)],
                    Quantity: _random.Next(1, 10)
                );

                await _publishEndpoint.Publish(message, stoppingToken);

                _logger.LogInformation("📦 Published order: {OrderId} for product: {ProductName} with quantity: {Quantity}",
                    message.OrderId, message.ProductName, message.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error publishing message");
            }

            // Wait between 1 and 3 seconds before publishing next message
            await Task.Delay(_random.Next(1000, 3000), stoppingToken);
        }
    }
}

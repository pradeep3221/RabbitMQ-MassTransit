using MassTransit;
using rabbitmq.producer.api.Contracts;

namespace rabbitmq.producer.worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IBus _bus;
    private readonly Random _random = new();

    public Worker(ILogger<Worker> logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = new OrderSubmitted(
                    OrderId: Guid.NewGuid(),
                    ProductName: $"Product-{_random.Next(1, 100)}",
                    Quantity: _random.Next(1, 10)
                );

                await _bus.Publish(message, stoppingToken);

                _logger.LogInformation("Published order: {OrderId} for product: {ProductName} with quantity: {Quantity}",
                    message.OrderId, message.ProductName, message.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message");
            }

            await Task.Delay(2000, stoppingToken);
        }
    }
}

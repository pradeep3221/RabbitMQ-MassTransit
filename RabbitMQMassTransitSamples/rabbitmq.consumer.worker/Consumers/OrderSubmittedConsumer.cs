using MassTransit;
using rabbitmq.producer.api.Contracts;

namespace rabbitmq.consumer.worker.Consumers;

/// <summary>
/// Consumer for OrderSubmitted messages
/// </summary>
public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    private readonly ILogger<OrderSubmittedConsumer> _logger;

    public OrderSubmittedConsumer(ILogger<OrderSubmittedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "📦 Received OrderSubmitted - OrderId: {OrderId}, Product: {ProductName}, Quantity: {Quantity}",
            message.OrderId, message.ProductName, message.Quantity);

        try
        {
            // Simulate processing the order
            await ProcessOrder(message);

            _logger.LogInformation(
                "✅ Successfully processed order - OrderId: {OrderId}",
                message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Failed to process order - OrderId: {OrderId}",
                message.OrderId);

            // Re-throw to let MassTransit handle retry logic
            throw;
        }
    }

    private async Task ProcessOrder(OrderSubmitted order)
    {
        // Simulate some processing time
        await Task.Delay(500);

        // Here you would typically:
        // - Validate the order
        // - Update inventory
        // - Create order record in database
        // - Send confirmation email
        // - etc.

        _logger.LogInformation(
            "🔄 Processing order for {ProductName} (Qty: {Quantity})...",
            order.ProductName, order.Quantity);
    }
}


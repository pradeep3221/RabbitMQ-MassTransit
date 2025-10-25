using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using rabbitmq.producer.api.Contracts;
using rabbitmq.producer.api.Persistence;

namespace rabbitmq.producer.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderController> _logger;
    private readonly OutboxDbContext _dbContext;
    private readonly bool _useTransactionalOutbox;

    public OrderController(
        IPublishEndpoint publishEndpoint, 
        ILogger<OrderController> logger,
        OutboxDbContext dbContext,
        IConfiguration configuration)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _dbContext = dbContext;
        _useTransactionalOutbox = configuration.GetValue<bool>("UseTransactionalOutbox");
    }

    /// <summary>
    /// Publishes an OrderSubmitted message to RabbitMQ
    /// </summary>
    /// <param name="productName">Name of the product</param>
    /// <param name="quantity">Quantity ordered</param>
    /// <returns>Order details</returns>
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitOrderRequest request)
    {
        var orderId = Guid.NewGuid();
        
        var orderSubmitted = new OrderSubmitted(
            orderId,
            request.ProductName,
            request.Quantity
        );

        _logger.LogInformation(
            "Publishing OrderSubmitted message - OrderId: {OrderId}, Product: {ProductName}, Quantity: {Quantity}",
            orderId, request.ProductName, request.Quantity);

        try
        {
            if (_useTransactionalOutbox)
            {
                // Use EF transaction with outbox
                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {
                    // Publish the message through the outbox
                    await _publishEndpoint.Publish(orderSubmitted);

                    // Commit the transaction - this will also store the message in the outbox
                    await transaction.CommitAsync();
                }

                _logger.LogInformation(
                    "Successfully published OrderSubmitted message using outbox - OrderId: {OrderId}",
                    orderId);
            }
            else
            {
                // Direct in-memory publishing
                await _publishEndpoint.Publish(orderSubmitted);

                _logger.LogInformation(
                    "Successfully published OrderSubmitted message in-memory - OrderId: {OrderId}",
                    orderId);
            }

            return Ok(new
            {
                Message = "Order submitted successfully",
                OrderId = orderId,
                ProductName = request.ProductName,
                Quantity = request.Quantity,
                Status = _useTransactionalOutbox ? "Published to outbox" : "Published in-memory"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish OrderSubmitted message - OrderId: {OrderId}",
                orderId);

            return StatusCode(500, new
            {
                Message = "Failed to submit order",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Publishes multiple test orders to RabbitMQ using the transactional outbox
    /// </summary>
    /// <param name="count">Number of test orders to publish</param>
    /// <returns>Summary of published orders</returns>
    [HttpPost("submit-batch")]
    public async Task<IActionResult> SubmitBatchOrders([FromQuery] int count = 5)
    {
        if (count <= 0 || count > 100)
        {
            return BadRequest("Count must be between 1 and 100");
        }

        var publishedOrders = new List<object>();
        var products = new[] { "Laptop", "Mouse", "Keyboard", "Monitor", "Headphones" };

        _logger.LogInformation("Starting batch publish of {Count} orders", count);

        try
        {
            if (_useTransactionalOutbox)
            {
                // Start a single transaction for the entire batch
                using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                {
                    for (int i = 0; i < count; i++)
                    {
                        var orderId = Guid.NewGuid();
                        var productName = products[i % products.Length];
                        var quantity = Random.Shared.Next(1, 10);

                        var orderSubmitted = new OrderSubmitted(orderId, productName, quantity);

                        // Publish the message through the outbox
                        await _publishEndpoint.Publish(orderSubmitted);

                        _logger.LogInformation(
                            "Published order {Index}/{Total} to outbox - OrderId: {OrderId}, Product: {ProductName}, Quantity: {Quantity}",
                            i + 1, count, orderId, productName, quantity);

                        publishedOrders.Add(new
                        {
                            OrderId = orderId,
                            ProductName = productName,
                            Quantity = quantity
                        });
                    }

                    // Commit the transaction - this will store all messages in the outbox
                    await transaction.CommitAsync();
                }
            }
            else
            {
                // Direct in-memory publishing for each message
                for (int i = 0; i < count; i++)
                {
                    var orderId = Guid.NewGuid();
                    var productName = products[i % products.Length];
                    var quantity = Random.Shared.Next(1, 10);

                    var orderSubmitted = new OrderSubmitted(orderId, productName, quantity);

                    // Direct publish without outbox
                    await _publishEndpoint.Publish(orderSubmitted);

                    _logger.LogInformation(
                        "Published order {Index}/{Total} in-memory - OrderId: {OrderId}, Product: {ProductName}, Quantity: {Quantity}",
                        i + 1, count, orderId, productName, quantity);

                    publishedOrders.Add(new
                    {
                        OrderId = orderId,
                        ProductName = productName,
                        Quantity = quantity
                    });
                }
            }

            _logger.LogInformation(
                "Batch publish completed - {SuccessCount}/{TotalCount} orders published successfully",
                publishedOrders.Count, count);

            return Ok(new
            {
                Message = "Batch orders submitted",
                TotalRequested = count,
                SuccessfullyPublished = publishedOrders.Count,
                Orders = publishedOrders,
                Status = _useTransactionalOutbox ? "Published to outbox" : "Published in-memory"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process batch orders");
            return StatusCode(500, new
            {
                Message = "Failed to submit batch orders",
                Error = ex.Message
            });
        }
    }
}

/// <summary>
/// Request model for submitting an order
/// </summary>
public record SubmitOrderRequest(string ProductName, int Quantity);


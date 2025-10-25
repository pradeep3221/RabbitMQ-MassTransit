using MassTransit;
using Microsoft.AspNetCore.Mvc;
using rabbitmq.producer.api.Contracts;

namespace rabbitmq.producer.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IPublishEndpoint publishEndpoint, ILogger<OrderController> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
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
            // Publish the message to RabbitMQ
            await _publishEndpoint.Publish(orderSubmitted);

            _logger.LogInformation(
                "Successfully published OrderSubmitted message - OrderId: {OrderId}",
                orderId);

            return Ok(new
            {
                Message = "Order submitted successfully",
                OrderId = orderId,
                ProductName = request.ProductName,
                Quantity = request.Quantity,
                Status = "Published to RabbitMQ"
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
    /// Publishes multiple test orders to RabbitMQ
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

        for (int i = 0; i < count; i++)
        {
            var orderId = Guid.NewGuid();
            var productName = products[i % products.Length];
            var quantity = Random.Shared.Next(1, 10);

            var orderSubmitted = new OrderSubmitted(orderId, productName, quantity);

            try
            {
                await _publishEndpoint.Publish(orderSubmitted);

                _logger.LogInformation(
                    "Published order {Index}/{Total} - OrderId: {OrderId}, Product: {ProductName}, Quantity: {Quantity}",
                    i + 1, count, orderId, productName, quantity);

                publishedOrders.Add(new
                {
                    OrderId = orderId,
                    ProductName = productName,
                    Quantity = quantity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish order {Index}/{Total} - OrderId: {OrderId}",
                    i + 1, count, orderId);
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
            Orders = publishedOrders
        });
    }
}

/// <summary>
/// Request model for submitting an order
/// </summary>
public record SubmitOrderRequest(string ProductName, int Quantity);


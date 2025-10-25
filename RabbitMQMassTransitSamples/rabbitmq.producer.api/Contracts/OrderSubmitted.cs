namespace rabbitmq.producer.api.Contracts;

/// <summary>
/// Message contract representing an order submission event
/// </summary>
public record OrderSubmitted(Guid OrderId, string ProductName, int Quantity);


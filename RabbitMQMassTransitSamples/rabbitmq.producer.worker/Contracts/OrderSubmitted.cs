namespace rabbitmq.producer.api.Contracts;

/// <summary>
/// Message contract representing an order submission event
/// NOTE: This namespace must match the producer's contract namespace for MassTransit message routing
/// </summary>
public record OrderSubmitted(Guid OrderId, string ProductName, int Quantity);
using MassTransit;
using rabbitmq.producer.worker;
using rabbitmq.producer.api.Contracts;

var builder = Host.CreateApplicationBuilder(args);

// Get RabbitMQ configuration
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
var exchangeName = builder.Configuration["RabbitMQ:ExchangeName"] ?? "orders-exchange";

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username(rabbitMqUsername);
            h.Password(rabbitMqPassword);
        });

        // Configure message topology
        cfg.Message<OrderSubmitted>(e =>
        {
            e.SetEntityName(exchangeName); // Set the exchange name for all OrderSubmitted messages
        });

        cfg.Publish<OrderSubmitted>(e =>
        {
            e.ExchangeType = "topic"; // Match the consumer's exchange type
        });
    });
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 RabbitMQ Producer Worker starting...");
logger.LogInformation("📡 Connecting to RabbitMQ at {Host} with username: {Username}", rabbitMqHost, rabbitMqUsername);
logger.LogInformation("📨 Publishing OrderSubmitted messages to exchange: {ExchangeName}", exchangeName);

host.Run();

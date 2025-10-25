using MassTransit;
using rabbitmq.consumer.worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

// Get RabbitMQ configuration
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
var exchangeName = builder.Configuration["RabbitMQ:ExchangeName"] ?? "orders-exchange";
var queueName = builder.Configuration["RabbitMQ:QueueName"] ?? "order-submitted-queue";

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add the consumer
    x.AddConsumer<OrderSubmittedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqHost, "/", h =>
        {
            h.Username(rabbitMqUsername);
            h.Password(rabbitMqPassword);
        });

        // Configure the receive endpoint for the consumer with specific queue
        cfg.ReceiveEndpoint(queueName, e =>
        {
            // Bind the queue to the specific exchange
            e.Bind(exchangeName, s =>
            {
                s.ExchangeType = "topic";
                s.RoutingKey = "order-submitted-queue"; //"#"; // Subscribe to all messages on this exchange
            });

            e.ConfigureConsumer<OrderSubmittedConsumer>(context);

            // Optional: Configure retry policy
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });
    });
});

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 RabbitMQ Consumer Worker starting...");
logger.LogInformation("📡 Connecting to RabbitMQ at {Host} with username: {Username}", rabbitMqHost, rabbitMqUsername);
logger.LogInformation("👂 Listening for OrderSubmitted messages on queue: {QueueName}", queueName);
logger.LogInformation("📬 Bound to exchange: {ExchangeName}", exchangeName);

host.Run();

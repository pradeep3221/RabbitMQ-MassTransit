using MassTransit;

namespace rabbitmq.producer.api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure MassTransit with RabbitMQ
            var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
            var exchangeName = builder.Configuration["RabbitMQ:ExchangeName"] ?? "orders-exchange";
            var queueName = builder.Configuration["RabbitMQ:QueueName"] ?? "order-submitted-queue";

            builder.Services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqHost, "/", h =>
                    {
                        h.Username(rabbitMqUsername);
                        h.Password(rabbitMqPassword);
                    });

                    // Configure message topology to use specific exchange and queue
                    cfg.Message<rabbitmq.producer.api.Contracts.OrderSubmitted>(e =>
                    {
                        e.SetEntityName(exchangeName); // Set the exchange name
                    });

                    // Configure publish topology
                    cfg.Publish<rabbitmq.producer.api.Contracts.OrderSubmitted>(e =>
                    {
                        e.ExchangeType = "topic"; // Use topic exchange for routing flexibility
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            var app = builder.Build();

            // Log MassTransit connection status
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("MassTransit configured with RabbitMQ at {Host}", rabbitMqHost);
            logger.LogInformation("RabbitMQ connection established with username: {Username}", rabbitMqUsername);
            logger.LogInformation("Publishing to Exchange: {ExchangeName}, Queue: {QueueName}", exchangeName, queueName);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

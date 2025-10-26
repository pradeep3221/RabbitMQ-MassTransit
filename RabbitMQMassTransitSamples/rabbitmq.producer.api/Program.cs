using MassTransit;
using Microsoft.EntityFrameworkCore;
using rabbitmq.producer.api.Persistence;

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

            // Configure SQL Server for the transactional outbox with retry logic
            builder.Services.AddDbContext<OutboxDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)));

 // Configure MassTransit with RabbitMQ and optional outbox
            var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
            var exchangeName = builder.Configuration["RabbitMQ:ExchangeName"] ?? "orders-exchange";
            var queueName = builder.Configuration["RabbitMQ:QueueName"] ?? "order-submitted-queue";
            var useTransactionalOutbox = builder.Configuration.GetValue<bool>("UseTransactionalOutbox");

            builder.Services.AddMassTransit(x =>
            {
                if (useTransactionalOutbox)
                {
                    // Configure the Entity Framework Core outbox when enabled
                    x.AddEntityFrameworkOutbox<OutboxDbContext>(o =>
                    {
                        // Configure the outbox to use SQL Server
                        o.UseSqlServer();

                        // Configure the outbox options
                        o.QueryDelay = TimeSpan.FromSeconds(1); // How often to query for outbox messages
                        o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30); // How long to store sent message IDs
                    });
                }

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqHost, "/", h =>
                    {
                        h.Username(rabbitMqUsername);
                        h.Password(rabbitMqPassword);
                    });

                    // Configure message topology to use specific exchange
                    cfg.Message<rabbitmq.producer.api.Contracts.OrderSubmitted>(e =>
                    {
                        e.SetEntityName(exchangeName);
                    });

                    // Configure publish topology
                    cfg.Publish<rabbitmq.producer.api.Contracts.OrderSubmitted>(e =>
                    {
                        e.ExchangeType = "topic"; // Use topic exchange with default routing
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

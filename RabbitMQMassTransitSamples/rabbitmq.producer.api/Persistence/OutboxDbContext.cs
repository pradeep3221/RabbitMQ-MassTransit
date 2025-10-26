using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace rabbitmq.producer.api.Persistence;

public class OutboxDbContext : DbContext
{
    private readonly ILogger<OutboxDbContext> _logger;

    public OutboxDbContext(DbContextOptions<OutboxDbContext> options, ILogger<OutboxDbContext> logger) : base(options)
    {
        _logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _logger.LogInformation("Configuring outbox tables in OnModelCreating");

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        _logger.LogInformation("Outbox tables configuration completed");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving changes to database...");
            var result = await base.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully saved {Count} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }
}
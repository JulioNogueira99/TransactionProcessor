using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TransactionProcessor.Application.Interfaces;

namespace TransactionProcessor.Infrastructure.Outbox;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatch(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox worker loop crashed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessBatch(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var messages = await outbox.GetPendingAsync(take: 20, ct);

        foreach (var msg in messages)
        {
            try
            {
                await publisher.PublishAsync(msg.Type, msg.Payload, ct);

                await outbox.MarkProcessedAsync(msg.Id, DateTimeOffset.UtcNow, ct);
                await uow.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                var attempts = msg.Attempts + 1;

                var delaySeconds = Math.Min(60, Math.Pow(2, attempts));
                var nextAttempt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);

                _logger.LogWarning(ex, "Failed to publish outbox {Id}. Attempts={Attempts}. NextAttempt={NextAttempt}",
                    msg.Id, attempts, nextAttempt);

                await outbox.MarkFailedAsync(msg.Id, attempts, nextAttempt, ex.Message, ct);
                await uow.CommitAsync(ct);
            }
        }
    }
}

using Microsoft.Extensions.Logging;
using TransactionProcessor.Application.Interfaces;

namespace TransactionProcessor.Infrastructure.Outbox;

public sealed class LoggingEventPublisher : IEventPublisher
{
    private readonly ILogger<LoggingEventPublisher> _logger;

    public LoggingEventPublisher(ILogger<LoggingEventPublisher> logger)
        => _logger = logger;

    public Task PublishAsync(string type, string payload, CancellationToken ct)
    {
        _logger.LogInformation("OUTBOX PUBLISH => Type={Type} Payload={Payload}", type, payload);
        return Task.CompletedTask;
    }
}
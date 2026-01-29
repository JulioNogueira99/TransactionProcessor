using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransactionProcessor.Infrastructure.Outbox;

namespace TransactionProcessor.Api.Health;

public sealed class OutboxCircuitBreakerHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var state = OutboxPublishPolicy.Breaker.CircuitState.ToString();

        return Task.FromResult(
            state == "Open"
                ? HealthCheckResult.Unhealthy($"Outbox circuit breaker is OPEN")
                : HealthCheckResult.Healthy($"Outbox circuit breaker is {state}")
        );
    }
}

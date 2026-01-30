using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;

namespace TransactionProcessor.Infrastructure.Outbox;

public static class OutboxPublishPolicy
{
    private static readonly IAsyncPolicy _retry =
        Policy.Handle<Exception>()
              .WaitAndRetryAsync(
                  retryCount: 3,
                  sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)));

    private static readonly AsyncCircuitBreakerPolicy _breaker =
        Policy.Handle<Exception>()
              .CircuitBreakerAsync(
                  exceptionsAllowedBeforeBreaking: 5,
                  durationOfBreak: TimeSpan.FromSeconds(30));

    public static IAsyncPolicy Wrap => Policy.WrapAsync(_retry, _breaker);

    public static AsyncCircuitBreakerPolicy Breaker => _breaker;
}

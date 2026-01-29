using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace TransactionProcessor.Infrastructure.Outbox;

public static class OutboxPublishPolicies
{
    public static readonly AsyncCircuitBreakerPolicy CircuitBreaker =
        Policy.Handle<Exception>()
              .CircuitBreakerAsync(
                  exceptionsAllowedBeforeBreaking: 5,
                  durationOfBreak: TimeSpan.FromSeconds(30));

    public static AsyncRetryPolicy Retry =
        Policy.Handle<Exception>()
              .WaitAndRetryAsync(
                  retryCount: 3,
                  sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))
              );

    public static IAsyncPolicy PublishPolicy =>
        Policy.WrapAsync(Retry, CircuitBreaker);
}

using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Resilience
{
    public static class ResiliencePolicies
    {
        /// <summary>
        /// Retry 3 lần với exponential backoff: 1s, 2s, 4s
        /// Chỉ retry khi gặp transient error (5xx, network error)
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger? logger = null)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()  // 5xx và network error
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        // Log mỗi lần retry để debug
                        logger?.LogWarning(
                            "Retry {Attempt} after {Delay}s. Reason: {Reason}",
                            retryAttempt, timespan.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });
        }

        /// <summary>
        /// Circuit Breaker: mở sau 5 lần fail liên tiếp trong 30s
        /// Giữ trạng thái Open trong 30s trước khi Half-Open
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger? logger = null)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, duration) =>
                    {
                        // Circuit mở — log alert để ops team biết
                        logger?.LogError(
                            "Circuit OPEN for {Duration}s: {Message}",
                            duration.TotalSeconds, exception.Exception?.Message ?? exception.Result?.StatusCode.ToString());
                    },
                    onReset: () => logger?.LogInformation("Circuit CLOSED — service recovered"),
                    onHalfOpen: () => logger?.LogInformation("Circuit HALF-OPEN — testing..."));
        }
    }

}

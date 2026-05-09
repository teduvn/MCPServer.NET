using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderManagement.Application.Common.Behaviors
{
    public class PerformanceBehavior<TRequest, TResponse>(
     ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
     : IPipelineBehavior<TRequest, TResponse>
     where TRequest : IRequest<TResponse>
    {
        // Ngưỡng cảnh báo: command > 500ms, query > 200ms
        private const int SlowCommandThresholdMs = 500;


        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await next();
            stopwatch.Stop();


            var elapsed = stopwatch.ElapsedMilliseconds;


            if (elapsed > SlowCommandThresholdMs)
            {
                // WARNING — không phải ERROR: chậm nhưng không crash
                logger.LogWarning(
                    "Slow Request: {RequestName} | Elapsed: {Elapsed}ms | Threshold: {Threshold}ms | Data: {@Request}",
                    typeof(TRequest).Name, elapsed, SlowCommandThresholdMs, request);
            }


            return response;
        }
    }

}

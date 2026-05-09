using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderManagement.Application.Common.Behaviors
{
    // Generic constraint: áp dụng cho TẤT CẢ command và query
    public class LoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly ICorrelationIdService _correlationIdService;

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger,
            ICorrelationIdService correlationIdService)
        {
            _logger = logger;
            _correlationIdService = correlationIdService;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var correlationId = _correlationIdService.CorrelationId;


            // Log khi bắt đầu — dùng structured property, không dùng string interpolation
            _logger.LogInformation(
                "Handling {RequestName} | CorrelationId: {CorrelationId} | Data: {@Request}",
                requestName, correlationId, request);


            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Gọi behavior/handler tiếp theo trong pipeline
                var response = await next();

                stopwatch.Stop();

                // Log khi thành công
                _logger.LogInformation(
                    "Handled {RequestName} successfully | CorrelationId: {CorrelationId} | Elapsed: {Elapsed}ms",
                    requestName, correlationId, stopwatch.ElapsedMilliseconds);


                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Log khi thất bại — WARNING cho business error, ERROR cho exception
                _logger.LogError(ex,
                    "Handling {RequestName} failed | CorrelationId: {CorrelationId} | Elapsed: {Elapsed}ms",
                    requestName, correlationId, stopwatch.ElapsedMilliseconds);


                throw; // Re-throw để Error Handling Middleware xử lý

            }
        }
    }

}

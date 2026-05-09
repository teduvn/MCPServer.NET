using MediatR;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Behaviors
{
    // Constraint: chỉ áp dụng cho TRequest implement ITransactionalCommand
    public class TransactionBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>, ITransactionalCommand
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

        public TransactionBehavior(
            IUnitOfWork unitOfWork,
            ILogger<TransactionBehavior<TRequest, TResponse>> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogInformation(
                "[Transaction] Beginning transaction for {RequestName}", requestName);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Chạy handler bên trong transaction
                var response = await next();

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "[Transaction] Transaction committed for {RequestName}", requestName);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Transaction] Transaction rolled back for {RequestName}", requestName);

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }

}

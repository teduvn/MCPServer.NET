using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Nếu không có validator nào được đăng ký → bỏ qua, gọi handler
            if (!_validators.Any())
                return await next();

            // Chạy tất cả validator
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            // Gom tất cả lỗi lại
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            // Nếu có lỗi → throw ValidationException
            if (failures.Count != 0)
                throw new ValidationException(failures);

            // Không có lỗi → gọi handler tiếp theo
            return await next();
        }
    }

}

using MediatR;
using OrderManagement.Application.Common.Observability;
using System.Diagnostics;

namespace OrderManagement.Application.Common.Behaviors
{
    public class TracingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;


            // Span mới tự động trở thành child của span đang active
            // Nếu McpServer đã tạo span cho tool call, span này là con của nó
            using var activity = OmsActivitySource.Instance
                .StartActivity($"MediatR: {requestName}");


            if (activity is not null)
            {
                activity.SetTag("mediator.request", requestName);
                // Phân biệt query (readonly) vs command (write)
                activity.SetTag("mediator.type",
                    requestName.EndsWith("Query") ? "query" : "command");
            }


            try
            {
                var response = await next();
                activity?.SetStatus(ActivityStatusCode.Ok);
                return response;
            }
            catch (Exception ex)
            {
                activity?.AddException(ex);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw; // Không nuốt exception — để handler phía trên xử lý
            }
        }
    }

}

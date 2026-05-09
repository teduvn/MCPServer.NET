using Microsoft.AspNetCore.Authorization;
using OrderManagement.Domain.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OrderManagement.WebAPI.Authorization
{
    // Requirement — chỉ là marker, không có logic
    public sealed class MustOwnOrderRequirement : IAuthorizationRequirement { }

    // Handler — chứa logic kiểm tra
    public sealed class MustOwnOrderHandler(
        IOrderRepository orderRepository)
        : AuthorizationHandler<MustOwnOrderRequirement>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MustOwnOrderRequirement requirement)
        {
            // Lấy orderId từ route parameter
            var routeData = context.Resource as HttpContext;
            var orderIdStr = routeData?.GetRouteValue("id")?.ToString();

            if (!Guid.TryParse(orderIdStr, out var orderId))
            {
                context.Fail();
                return;
            }

            // Lấy userId từ claim
            var userIdStr = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                context.Fail();
                return;
            }

            // Check ownership
            var order = await orderRepository.GetByIdAsync(orderId);
            if (order is not null && order.CustomerId == userId)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }

}

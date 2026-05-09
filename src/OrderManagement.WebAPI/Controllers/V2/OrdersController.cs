using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Queries;
using OrderManagement.WebAPI.Extensions;

namespace OrderManagement.WebAPI.Controllers.V2
{
    [ApiController]
    [Authorize]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/orders")]
    public class OrdersController(ISender sender) : BaseApiController(sender)
    {
        // V2 trả về PagedResult thay vì plain list
        //[HttpGet]
        //[ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), StatusCodes.Status200OK)]
        //public async Task<IActionResult> GetOrders(
        //    [FromQuery] Guid customerId,
        //    [FromQuery] int page = 1,
        //    [FromQuery] int pageSize = 20,
        //    CancellationToken ct = default)
        //{
        //    var query = new GetOrdersPagedQuery
        //    {
        //        CustomerId = customerId,
        //        Page = page,
        //        PageSize = pageSize
        //    };
        //    var result = await _sender.Send(query, ct);
        //    return Ok(result);
        //}
    }

}

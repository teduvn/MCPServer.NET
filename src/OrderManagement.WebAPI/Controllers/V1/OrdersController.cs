using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Queries;
using OrderManagement.WebAPI.Extensions;

namespace OrderManagement.WebAPI.Controllers.V1
{
    [ApiController]
    //[Authorize]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/orders")]
    public class OrdersController(ISender sender) : BaseApiController(sender)
    {
        // POST /api/orders
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            var shippingAddress = new AddressDto()
            {
                Street = request.ShippingAddress.Street,
                City = request.ShippingAddress.City,
                Province = request.ShippingAddress.Province,
                Country = request.ShippingAddress.Country,
                PostalCode = request.ShippingAddress.PostalCode
            };
            var command = new PlaceOrderCommand
            {
                CustomerId = request.CustomerId,
                ShippingAddress = shippingAddress,
                Items = request.Items.Select(i => new OrderItemDto
                                                        {
                                                            ProductId = i.ProductId,
                                                            Quantity = i.Quantity
                                                        }).ToList()
                                                };

            var result = await sender.Send(command);

            if (result.IsFailure)
                return result.ToProblemDetails();

            return CreatedAtAction(nameof(GetOrder), new { id = result.Value }, new { orderId = result.Value });
        }

        // GET /api/orders/{id}
        [HttpGet("{id:guid}")]
        //[Authorize(Policy = "MustOwnOrder")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var query = new GetOrderByIdQuery(id);
            var result = await sender.Send(query);

            if (result.IsFailure)
                return result.ToProblemDetails();

            return result.Value is null ? NotFound() : Ok(result.Value);
        }

        //[HttpPut("{id}/cancel")]
        //[Authorize(Policy = "MustOwnOrder")]
        //public async Task<IActionResult> Cancel(Guid id)
        //    => Ok(await sender.Send(new CancelOrder(id)));

    }

}

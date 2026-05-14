using MediatR;
using ModelContextProtocol.Server;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Queries;
using System.ComponentModel;

namespace OrderManagement.McpServer.OrderTools
{
    [McpServerToolType]  // đánh dấu class này chứa MCP tools
    public class OrderTools
    {
        private readonly IMediator _mediator;

        public OrderTools(IMediator mediator)
        {
            _mediator = mediator;
        }


        [McpServerTool(Name = "get_order")]
        [Description("Lấy thông tin chi tiết một đơn hàng theo ID. " +
                     "Trả về: order ID, trạng thái, danh sách sản phẩm, " +
                     "tổng tiền, tên khách hàng và ngày tạo.")]
        public async Task<OrderDto?> GetOrder(
            [Description("ID của đơn hàng cần xem, dạng GUID")] Guid orderId)
        {
            var query = new GetOrderByIdQuery(orderId);
            var result =  await _mediator.Send(query);
            if(result.IsFailure)
            {
                return null;
            }
            return result.Value!;
        }
    }

}

using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace OrderManagement.McpServer.Prompts
{
    [McpServerPromptType]
    public class OrderPrompts
    {
        [McpServerPrompt(Name = "summarize_draft_orders")]
        [Description("Tóm tắt danh sách order đang chờ xử lý theo format chuẩn OMS")]
        public static IEnumerable<PromptMessage> SummarizeDraftOrders()
        {
            return
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock
                    {
                        Text = """
                            Hãy thực hiện các bước sau:
                            1. Gọi tool list_orders với tham số status='Draft'
                            2. Tóm tắt kết quả theo format:
                               - Tổng số order đang chờ
                               - Top 3 khách hàng có nhiều order nhất
                               - Order có giá trị cao nhất
                               - Cảnh báo nếu có order quá 48h chưa xử lý
                            3. Kết thúc bằng gợi ý hành động tiếp theo
                            """
                    }
                }
            ];
        }

        [McpServerPrompt(Name = "draft_order_confirmation")]
        [Description("Soạn email xác nhận đơn hàng gửi cho khách")]
        public static IEnumerable<PromptMessage> DraftOrderConfirmation(
            [Description("Mã order cần xác nhận, ví dụ: ORD-2024-001")]
            string orderId,
            [Description("Ngôn ngữ email: 'vi' hoặc 'en', mặc định là 'vi'")]
            string language = "vi")
        {
            var langInstruction = language == "en"
                ? "Write the email in English, professional tone."
                : "Viết email bằng tiếng Việt, giọng văn lịch sự và chuyên nghiệp.";
            return
            [
                new PromptMessage
                {
                    Role = Role.Assistant,
                    Content = new TextContentBlock
                    {
                        Text = "Bạn là nhân viên CSKH chuyên nghiệp của công ty. " +
                               "Hãy soạn thảo email xác nhận đơn hàng một cách chính xác và chu đáo."
                    }
                },
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock
                    {
                        Text = $"""
                            {langInstruction}


                            Thực hiện theo thứ tự:
                            1. Gọi tool get_order với orderId = "{orderId}" để lấy chi tiết
                            2. Soạn email xác nhận bao gồm:
                               - Lời chào khách hàng (dùng tên từ order)
                               - Xác nhận mã đơn hàng và ngày đặt
                               - Danh sách sản phẩm và số lượng
                               - Tổng giá trị và thông tin thanh toán
                               - Thời gian giao hàng dự kiến
                               - Thông tin liên hệ hỗ trợ
                            """
                    }
                }
            ];
        }

    }

}

using OrderManagement.Application.Common.Interfaces;

namespace OrderManagement.McpServer.Services
{
    public class McpContextAccessor : IMcpContextAccessor
    {
        // AsyncLocal giống HttpContextAccessor — mỗi async flow có giá trị riêng
        private static readonly AsyncLocal<string?> _toolName = new();
        private static readonly AsyncLocal<string?> _sessionId = new();


        public string? CurrentToolName => _toolName.Value;
        public string? CurrentSessionId => _sessionId.Value;


        public void SetTool(string toolName, string? sessionId = null)
        {
            _toolName.Value = toolName;
            _sessionId.Value = sessionId;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Interfaces
{
    public interface IMcpContextAccessor
    {
        string? CurrentToolName { get; }
        string? CurrentSessionId { get; }

        void SetTool(string toolName, string? sessionId = null);
    }

}

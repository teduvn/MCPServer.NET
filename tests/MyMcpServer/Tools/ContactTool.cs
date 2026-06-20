using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MyMcpServer.Tools
{
    [McpServerToolType]
    public class ContactTool
    {
        [McpServerTool]
        [Description("Get contact information for support.")]
        public string GetContactInfo([Description("The name of the contact")] string name)
        {
            return $"You can contact {name} at contact@example.com";
        }
    }
}

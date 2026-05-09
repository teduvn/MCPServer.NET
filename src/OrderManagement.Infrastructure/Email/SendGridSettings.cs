using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Email
{
    public class SendGridSettings
    {
        public const string SectionName = "SendGrid";

        public string ApiKey { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "TEDU Order System";
    }
}

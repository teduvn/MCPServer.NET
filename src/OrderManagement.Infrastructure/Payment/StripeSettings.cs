using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Payment
{
    public class StripeSettings
    {
        public const string SectionName = "Stripe";

        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Email
{
    public sealed class EmailDeliveryException : Exception
    {
        public EmailDeliveryException(string message) : base(message) { }
        public EmailDeliveryException(string message, Exception inner) : base(message, inner) { }
    }

}

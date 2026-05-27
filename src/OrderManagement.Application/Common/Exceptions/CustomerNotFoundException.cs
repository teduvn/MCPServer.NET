using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Exceptions
{
    public class CustomerNotFoundException : AppException
    {
        public string CustomerId { get; set; }
        public CustomerNotFoundException(string customerId)
           : base($"Customer with ID '{customerId}' was not found.") { }
    }
}

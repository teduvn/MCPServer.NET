using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Common
{
    public class DomainException : Exception
    {
        public DomainException() { }

        public DomainException(string message) : base(message) { }
    }
}

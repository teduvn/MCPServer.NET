using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Exceptions
{
    /// <summary>
    /// Resource conflict — thường gặp với optimistic concurrency.
    /// → HTTP 409 Conflict
    /// </summary>
    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message) { }
    }

}

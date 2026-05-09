using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Exceptions
{
    // src/OrderManagement.Application/Exceptions/ForbiddenException.cs
    /// <summary>
    /// User đã xác thực nhưng không có quyền thực hiện action này.
    /// → HTTP 403 Forbidden
    /// </summary>
    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message) : base(message) { }
    }

}

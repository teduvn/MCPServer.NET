using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Exceptions
{
    /// <summary>
    /// Entity tồn tại trong domain nhưng không tìm thấy trong persistence.
    /// → HTTP 404 Not Found
    /// </summary>
    public class NotFoundException : AppException
    {
        public NotFoundException(string entityName, object key)
            : base($"{entityName} với id '{key}' không tồn tại.") { }
    }

}

using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Exceptions
{
    /// <summary>
    /// Base cho tất cả application-level exception.
    /// Middleware sẽ catch loại này để biết đây là 'expected error'
    /// — không phải technical failure.
    /// </summary>
    public abstract class AppException : Exception
    {
        protected AppException(string message) : base(message) { }
    }

}

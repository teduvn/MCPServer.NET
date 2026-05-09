using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Interfaces
{
    /// <summary>
    /// Cung cấp CorrelationId của request hiện tại.
    /// Được inject vào Application Layer — không phụ thuộc HttpContext.
    /// </summary>
    public interface ICorrelationIdService
    {
        string CorrelationId { get; }
    }

}

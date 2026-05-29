using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderManagement.Application.Common.Observability
{
    public static class OmsActivitySource
    {
        public const string Name = "OMS.Application";

        // Static singleton — OTel SDK yêu cầu dùng chung instance
        public static readonly ActivitySource Instance = new ActivitySource(Name);
    }

}

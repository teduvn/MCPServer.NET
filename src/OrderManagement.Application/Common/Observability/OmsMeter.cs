using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace OrderManagement.Application.Common.Observability
{
    public static class OmsMeter
    {
        public const string Name = "OMS.Application";
        public static readonly Meter Instance = new Meter(Name);


        // Đếm số lần mỗi tool được gọi, có label để phân biệt tool và status
        public static readonly Counter<long> ToolCallCount =
            Instance.CreateCounter<long>("mcp.tool.calls");


        // Đo latency — dùng Histogram để tính được P50/P95/P99
        public static readonly Histogram<double> ToolLatency =
            Instance.CreateHistogram<double>("mcp.tool.latency_ms", unit: "ms");
    }

}

using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.ValueObjects
{
    /// <summary>
    /// Value Object đại diện cho khu vực vận chuyển.
    /// Thay vì string 'ZONE_A', dùng object có behavior.
    /// </summary>
    public sealed record ShippingZone
    {
        public string Code { get; }
        public decimal RatePerKg { get; }

        private ShippingZone(string code, decimal ratePerKg)
        {
            Code = code;
            RatePerKg = ratePerKg;
        }

        // Static factory — tránh magic string rải rác trong codebase
        public static readonly ShippingZone Domestic = new("DOMESTIC", 15_000m);
        public static readonly ShippingZone Regional = new("REGIONAL", 25_000m);
        public static readonly ShippingZone International = new("INTL", 80_000m);
    }

}

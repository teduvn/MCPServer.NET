using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Authorization
{
    public static class Permissions
    {
        public static class Orders
        {
            public const string View = "Permissions.Orders.View";
            public const string Cancel = "Permissions.Orders.Cancel";
            public const string Manage = "Permissions.Orders.Manage";
        }


        public static class Reports
        {
            public const string ViewRevenue = "Permissions.Reports.ViewRevenue";
        }
    }

}

using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Authorization
{
    public static class PolicyRegistrar
    {
        public static void RegisterPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("CanCancelOrder", policy =>
                policy.RequireClaim("permission", Permissions.Orders.Cancel));


            options.AddPolicy("CanViewRevenue", policy =>
                policy.RequireClaim("permission",
                    Permissions.Reports.ViewRevenue,
                    Permissions.Orders.Manage)); // Manager cũng được xem revenue


            options.AddPolicy("CanManageOrders", policy =>
                policy.RequireClaim("permission", Permissions.Orders.Manage));
        }
    }

}

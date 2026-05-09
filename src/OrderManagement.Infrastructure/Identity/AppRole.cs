using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Identity
{
    /// <summary>
    /// AppRole là Identity role model.
    /// Nằm ở Infrastructure — extend thêm Description nếu cần.
    /// Tương ứng với role data đã seed ở bài 4.3.
    /// </summary>
    public class AppRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
    }

}

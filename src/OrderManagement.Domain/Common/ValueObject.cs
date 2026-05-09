using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Common
{
    // Record tự implement Equals() và GetHashCode() dựa trên tất cả property
    public abstract record ValueObject;
}

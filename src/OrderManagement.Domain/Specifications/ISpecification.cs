using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace OrderManagement.Domain.Specifications
{
    /// <summary>
    /// Interface cho Specification Pattern.
    /// T là loại entity cần query.
    /// </summary>
    public interface ISpecification<T>
    {
        /// <summary>
        /// Expression dung de build WHERE clause trong EF Core query.
        /// </summary>
        Expression<Func<T, bool>> Criteria { get; }

        /// <summary>
        /// Danh sach includes (eager loading). Moi phan tu la 1 navigation property.
        /// </summary>
        List<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// Sap xep theo field nao. null neu khong can sort.
        /// </summary>
        Expression<Func<T, object>>? OrderBy { get; }

        Expression<Func<T, object>>? OrderByDescending { get; }

        /// <summary>
        /// Phan trang. -1 = khong phan trang.
        /// </summary>
        int Take { get; }
        int Skip { get; }
        bool IsPagingEnabled { get; }
    }

}

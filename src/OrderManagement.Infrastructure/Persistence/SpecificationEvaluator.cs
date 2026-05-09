using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Specifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence
{
    public static class SpecificationEvaluator<T> where T : class
    {
        public static IQueryable<T> GetQuery(
            IQueryable<T> inputQuery,
            ISpecification<T> specification)
        {
            var query = inputQuery;

            // Áp WHERE clause từ Specification.Criteria
            query = query.Where(specification.Criteria);

            // Áp Include (eager loading) từ Specification.Includes
            query = specification.Includes
                .Aggregate(query, (curr, include) => curr.Include(include));

            // Áp OrderBy
            if (specification.OrderByDescending is not null)
                query = query.OrderByDescending(specification.OrderByDescending);
            else if (specification.OrderBy is not null)
                query = query.OrderBy(specification.OrderBy);

            // Áp Paging
            if (specification.IsPagingEnabled)
                query = query.Skip(specification.Skip).Take(specification.Take);

            return query;
        }
    }

}

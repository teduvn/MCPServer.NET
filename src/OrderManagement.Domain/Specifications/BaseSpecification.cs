using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace OrderManagement.Domain.Specifications
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        protected BaseSpecification()
            : this(_ => true)
        {
        }

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        public Expression<Func<T, bool>> Criteria { get; private set; }
        public List<Expression<Func<T, object>>> Includes { get; } = [];
        public Expression<Func<T, object>>? OrderBy { get; private set; }
        public Expression<Func<T, object>>? OrderByDescending { get; private set; }
        public int Take { get; private set; }
        public int Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        // Builder methods — trả về this để chaining
        protected void AddCriteria(Expression<Func<T, bool>> criteria)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var left = ReplaceParameter(Criteria, parameter);
            var right = ReplaceParameter(criteria, parameter);
            Criteria = Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left, right),
                parameter);
        }

        protected void AddInclude(Expression<Func<T, object>> includeExpr)
            => Includes.Add(includeExpr);

        protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpr)
            => OrderBy = orderByExpr;

        protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByExpr)
            => OrderByDescending = orderByExpr;

        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        private static Expression ReplaceParameter(
            Expression<Func<T, bool>> expression,
            ParameterExpression parameter)
            => new ReplaceParameterVisitor(expression.Parameters[0], parameter)
                .Visit(expression.Body)!;

        private sealed class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _source;
            private readonly ParameterExpression _target;

            public ReplaceParameterVisitor(
                ParameterExpression source,
                ParameterExpression target)
            {
                _source = source;
                _target = target;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _source ? _target : base.VisitParameter(node);
        }
    }

}

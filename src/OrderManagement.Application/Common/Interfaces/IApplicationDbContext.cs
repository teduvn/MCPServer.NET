using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Order> Orders { get; }
        DbSet<Product> Products { get; }
        DbSet<Customer> Customers { get; }
        DbSet<Voucher> Vouchers { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }

}

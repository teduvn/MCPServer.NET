using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Seeding
{
    public interface IDataSeeder
    {
        // Order: seeder có dependency phải chạy sau seeder khác
        int Order { get; }

        Task SeedAsync(CancellationToken ct = default);
    }

}

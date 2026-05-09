using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderManagement.Application.Common.Interfaces;
using System.Data;

namespace OrderManagement.Infrastructure.Persistence
{
    public sealed class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;


        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }


        public IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }

}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultValueToCustomerRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "RowVersion",
                table: "Customers",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "RowVersion",
                table: "Customers",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 0L);
        }
    }
}

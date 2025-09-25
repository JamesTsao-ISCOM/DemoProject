using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project01_movie_lease_system.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodToLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "Leases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "Leases");
        }
    }
}

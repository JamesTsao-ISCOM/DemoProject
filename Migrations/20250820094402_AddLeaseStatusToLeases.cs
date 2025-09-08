using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project01_movie_lease_system.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseStatusToLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Leases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_MemberId",
                table: "Reviews",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_MovieId",
                table: "Reviews",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_MemberId",
                table: "Leases",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_MovieId",
                table: "Leases",
                column: "MovieId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_Members_MemberId",
                table: "Leases",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Leases_Movies_MovieId",
                table: "Leases",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Members_MemberId",
                table: "Reviews",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Movies_MovieId",
                table: "Reviews",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leases_Members_MemberId",
                table: "Leases");

            migrationBuilder.DropForeignKey(
                name: "FK_Leases_Movies_MovieId",
                table: "Leases");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Members_MemberId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Movies_MovieId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_MemberId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_MovieId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Leases_MemberId",
                table: "Leases");

            migrationBuilder.DropIndex(
                name: "IX_Leases_MovieId",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Leases");
        }
    }
}

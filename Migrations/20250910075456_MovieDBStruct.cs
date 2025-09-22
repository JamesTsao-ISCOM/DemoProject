using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project01_movie_lease_system.Migrations
{
    /// <inheritdoc />
    public partial class MovieDBStruct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoWatchRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    LastPosition = table.Column<double>(type: "float", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    LastWatchedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VideoWat__3214EC07974BCD37", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoWatchRecords_Admins",
                        column: x => x.AdminId,
                        principalTable: "Admins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VideoWatchRecords_Videos",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoWatchRecords_AdminId",
                table: "VideoWatchRecords",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoWatchRecords_FileId",
                table: "VideoWatchRecords",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoWatchRecords");
        }
    }
}

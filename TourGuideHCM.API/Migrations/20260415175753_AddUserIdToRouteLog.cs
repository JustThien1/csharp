using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourGuideHCM.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToRouteLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "RouteLogs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteLogs_UserId",
                table: "RouteLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteLogs_Users_UserId",
                table: "RouteLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteLogs_Users_UserId",
                table: "RouteLogs");

            migrationBuilder.DropIndex(
                name: "IX_RouteLogs_UserId",
                table: "RouteLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RouteLogs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourGuideHCM.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaybackLogUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PlayedAt",
                table: "PlaybackLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayedAt",
                table: "PlaybackLogs");
        }
    }
}

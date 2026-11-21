using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwapShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekStartToAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Availabilities_UserId_DayOfWeek",
                table: "Availabilities");

            migrationBuilder.AddColumn<DateOnly>(
                name: "WeekStart",
                table: "Availabilities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_UserId_WeekStart_DayOfWeek",
                table: "Availabilities",
                columns: new[] { "UserId", "WeekStart", "DayOfWeek" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Availabilities_UserId_WeekStart_DayOfWeek",
                table: "Availabilities");

            migrationBuilder.DropColumn(
                name: "WeekStart",
                table: "Availabilities");

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_UserId_DayOfWeek",
                table: "Availabilities",
                columns: new[] { "UserId", "DayOfWeek" });
        }
    }
}

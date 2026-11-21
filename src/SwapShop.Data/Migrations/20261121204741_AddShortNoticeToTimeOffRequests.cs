using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwapShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShortNoticeToTimeOffRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsShortNotice",
                table: "TimeOffRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsShortNotice",
                table: "TimeOffRequests");
        }
    }
}

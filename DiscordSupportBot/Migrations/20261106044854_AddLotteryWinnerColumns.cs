using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordSupportBot.Migrations
{
    /// <inheritdoc />
    public partial class AddLotteryWinnerColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WinnerHistoryList",
                table: "Lottery",
                type: "TEXT",
                nullable: true,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "WinnerList",
                table: "Lottery",
                type: "TEXT",
                nullable: true,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WinnerHistoryList",
                table: "Lottery");

            migrationBuilder.DropColumn(
                name: "WinnerList",
                table: "Lottery");
        }
    }
}

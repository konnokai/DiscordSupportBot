using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordSupportBot.Migrations
{
    public partial class DeleteTimeChannel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelTimeId",
                table: "UpdateGuildInfo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "ChannelTimeId",
                table: "UpdateGuildInfo",
                nullable: false,
                defaultValue: 0ul);
        }
    }
}

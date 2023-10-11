using Microsoft.EntityFrameworkCore.Migrations;

namespace DiscordSupportBot.Migrations
{
    public partial class AddChannelNitroInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "ChannelNitroId",
                table: "UpdateGuildInfo",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<string>(
                name: "LastTwitterProfileURL",
                table: "UpdateGuildInfo",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "NoticeChangeAvatarChannelId",
                table: "UpdateGuildInfo",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<long>(
                name: "TwitterId",
                table: "UpdateGuildInfo",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelNitroId",
                table: "UpdateGuildInfo");

            migrationBuilder.DropColumn(
                name: "LastTwitterProfileURL",
                table: "UpdateGuildInfo");

            migrationBuilder.DropColumn(
                name: "NoticeChangeAvatarChannelId",
                table: "UpdateGuildInfo");

            migrationBuilder.DropColumn(
                name: "TwitterId",
                table: "UpdateGuildInfo");
        }
    }
}

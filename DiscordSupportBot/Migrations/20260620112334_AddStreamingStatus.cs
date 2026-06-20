using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordSupportBot.Migrations
{
    /// <inheritdoc />
    public partial class AddStreamingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableStreamingStatus",
                table: "GuildConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StreamingStatusTemplate",
                table: "GuildConfig",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableStreamingStatus",
                table: "GuildConfig");

            migrationBuilder.DropColumn(
                name: "StreamingStatusTemplate",
                table: "GuildConfig");
        }
    }
}

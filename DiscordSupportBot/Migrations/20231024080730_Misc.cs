using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordSupportBot.Migrations
{
    /// <inheritdoc />
    public partial class Misc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpdateGuildInfo");

            migrationBuilder.AddColumn<ulong>(
                name: "GuildId",
                table: "Lottery",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.CreateTable(
                name: "GuildConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AutoVoiceChannel = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelMemberId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelNitroId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TwitterId = table.Column<long>(type: "INTEGER", nullable: false),
                    LastTwitterProfileURL = table.Column<string>(type: "TEXT", nullable: true),
                    NoticeChangeAvatarChannelId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfig", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildConfig");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "Lottery");

            migrationBuilder.CreateTable(
                name: "UpdateGuildInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelMemberId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelNitroId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LastTwitterProfileURL = table.Column<string>(type: "TEXT", nullable: true),
                    NoticeChangeAvatarChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TwitterId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateGuildInfo", x => x.Id);
                });
        }
    }
}

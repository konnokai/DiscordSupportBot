using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordSupportBot.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTwitter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrustedGuild");

            migrationBuilder.DropColumn(
                name: "LastTwitterProfileURL",
                table: "GuildConfig");

            migrationBuilder.DropColumn(
                name: "NoticeChangeAvatarChannelId",
                table: "GuildConfig");

            migrationBuilder.DropColumn(
                name: "TwitterId",
                table: "GuildConfig");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastTwitterProfileURL",
                table: "GuildConfig",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "NoticeChangeAvatarChannelId",
                table: "GuildConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<long>(
                name: "TwitterId",
                table: "GuildConfig",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "TrustedGuild",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedGuild", x => x.Id);
                });
        }
    }
}

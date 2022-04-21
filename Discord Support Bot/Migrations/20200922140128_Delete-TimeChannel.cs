using Microsoft.EntityFrameworkCore.Migrations;

namespace Discord_Support_Bot.Migrations
{
    public partial class DeleteTimeChannel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrustedGuild",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustedGuild", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpdateGuildInfo",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelMemberId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateGuildInfo", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrustedGuild");

            migrationBuilder.DropTable(
                name: "UpdateGuildInfo");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Discord_Support_Bot.Migrations
{
    public partial class NCChannel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NCChannelCOD",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CODId = table.Column<string>(type: "TEXT", nullable: true),
                    Platform = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCChannelCOD", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NCChannelLottery",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Guid = table.Column<string>(type: "TEXT", nullable: true),
                    Context = table.Column<string>(type: "TEXT", nullable: true),
                    AwardContext = table.Column<string>(type: "TEXT", nullable: true),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxAward = table.Column<int>(type: "INTEGER", nullable: false),
                    ParticipantList = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCChannelLottery", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NCChannelCOD");

            migrationBuilder.DropTable(
                name: "NCChannelLottery");
        }
    }
}

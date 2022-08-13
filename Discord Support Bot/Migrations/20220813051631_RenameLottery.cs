using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Discord_Support_Bot.Migrations
{
    public partial class RenameLottery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NCChannelLottery");

            migrationBuilder.CreateTable(
                name: "Lottery",
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
                    table.PrimaryKey("PK_Lottery", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lottery");

            migrationBuilder.CreateTable(
                name: "NCChannelLottery",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AwardContext = table.Column<string>(type: "TEXT", nullable: true),
                    Context = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Guid = table.Column<string>(type: "TEXT", nullable: true),
                    MaxAward = table.Column<int>(type: "INTEGER", nullable: false),
                    ParticipantList = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCChannelLottery", x => x.Id);
                });
        }
    }
}

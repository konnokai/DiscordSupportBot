using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordSupportBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoCreatePrivateThreadBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoCreatePrivateThreadBinding",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ThreadId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoCreatePrivateThreadBinding", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoCreatePrivateThreadBinding_ChannelId_UserId",
                table: "AutoCreatePrivateThreadBinding",
                columns: new[] { "ChannelId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoCreatePrivateThreadBinding");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindIFBot.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingSubmissions",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotosJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MediaGroupId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingSubmissions", x => x.MessageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingSubmissions_CreatedAtUtc",
                table: "PendingSubmissions",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingSubmissions");
        }
    }
}

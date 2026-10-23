using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindIFBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminQueueReminderState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminQueueReminderStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminQueueReminderStates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminQueueReminderStates");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindIFBot.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelDailyStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "UserRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChannelDailyStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    BotUserCount = table.Column<int>(type: "int", nullable: false),
                    ChannelSubscriberCount = table.Column<int>(type: "int", nullable: false),
                    PostsCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDailyStatistics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_Status_PublishedAtUtc",
                table: "UserRequests",
                columns: new[] { "Status", "PublishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDailyStatistics_Date",
                table: "ChannelDailyStatistics",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelDailyStatistics");

            migrationBuilder.DropIndex(
                name: "IX_UserRequests_Status_PublishedAtUtc",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "UserRequests");
        }
    }
}

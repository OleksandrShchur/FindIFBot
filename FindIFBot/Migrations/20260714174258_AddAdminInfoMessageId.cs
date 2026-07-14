using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FindIFBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminInfoMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdminInfoMessageId",
                table: "UserRequests",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminInfoMessageId",
                table: "UserRequests");
        }
    }
}

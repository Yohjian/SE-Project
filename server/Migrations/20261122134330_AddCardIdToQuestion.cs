using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class AddCardIdToQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardId",
                table: "Questions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CardId",
                table: "Questions",
                column: "CardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Cards_CardId",
                table: "Questions",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Cards_CardId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_CardId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CardId",
                table: "Questions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class QuestionCardSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Cards_CardId",
                table: "Questions");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Cards_CardId",
                table: "Questions",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Cards_CardId",
                table: "Questions");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Cards_CardId",
                table: "Questions",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id");
        }
    }
}

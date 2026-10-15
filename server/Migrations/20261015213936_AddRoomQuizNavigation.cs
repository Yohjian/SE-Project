using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomQuizNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Rooms_QuizId",
                table: "Rooms",
                column: "QuizId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Quizzes_QuizId",
                table: "Rooms",
                column: "QuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Quizzes_QuizId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_QuizId",
                table: "Rooms");
        }
    }
}

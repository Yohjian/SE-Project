using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizSourceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceQuizId",
                table: "Quizzes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_SourceQuizId",
                table: "Quizzes",
                column: "SourceQuizId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Quizzes_SourceQuizId",
                table: "Quizzes",
                column: "SourceQuizId",
                principalTable: "Quizzes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Quizzes_SourceQuizId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_SourceQuizId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "SourceQuizId",
                table: "Quizzes");
        }
    }
}

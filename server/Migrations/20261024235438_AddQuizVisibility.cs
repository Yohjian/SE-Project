using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Quizzes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Quizzes");
        }
    }
}

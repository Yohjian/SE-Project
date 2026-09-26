using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class FixVocabularySetProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Sets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Sets");
        }
    }
}

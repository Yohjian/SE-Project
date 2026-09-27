using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class FixVocabularySetNameProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sets_SetID",
                table: "Cards");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Sets",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Sets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "SetID",
                table: "Cards",
                newName: "SetId");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Cards",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Cards_SetID",
                table: "Cards",
                newName: "IX_Cards_SetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sets_SetId",
                table: "Cards",
                column: "SetId",
                principalTable: "Sets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sets_SetId",
                table: "Cards");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Sets",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Sets",
                newName: "ID");

            migrationBuilder.RenameColumn(
                name: "SetId",
                table: "Cards",
                newName: "SetID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Cards",
                newName: "ID");

            migrationBuilder.RenameIndex(
                name: "IX_Cards_SetId",
                table: "Cards",
                newName: "IX_Cards_SetID");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sets_SetID",
                table: "Cards",
                column: "SetID",
                principalTable: "Sets",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

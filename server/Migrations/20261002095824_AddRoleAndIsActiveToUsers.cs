using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAndIsActiveToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Translation_Language_LanguageId",
                table: "Translation");

            migrationBuilder.DropForeignKey(
                name: "FK_Translation_Word_WordId",
                table: "Translation");

            migrationBuilder.DropForeignKey(
                name: "FK_Word_Category_CategoryId",
                table: "Word");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Word",
                table: "Word");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Translation",
                table: "Translation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Language",
                table: "Language");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Category",
                table: "Category");

            migrationBuilder.RenameTable(
                name: "Word",
                newName: "Words");

            migrationBuilder.RenameTable(
                name: "Translation",
                newName: "Translations");

            migrationBuilder.RenameTable(
                name: "Language",
                newName: "Languages");

            migrationBuilder.RenameTable(
                name: "Category",
                newName: "Categories");

            migrationBuilder.RenameIndex(
                name: "IX_Word_CategoryId",
                table: "Words",
                newName: "IX_Words_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Translation_WordId_LanguageId",
                table: "Translations",
                newName: "IX_Translations_WordId_LanguageId");

            migrationBuilder.RenameIndex(
                name: "IX_Translation_LanguageId",
                table: "Translations",
                newName: "IX_Translations_LanguageId");

            migrationBuilder.RenameIndex(
                name: "IX_Language_Code",
                table: "Languages",
                newName: "IX_Languages_Code");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Words",
                table: "Words",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Translations",
                table: "Translations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Languages",
                table: "Languages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Translations_Languages_LanguageId",
                table: "Translations",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Translations_Words_WordId",
                table: "Translations",
                column: "WordId",
                principalTable: "Words",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Words_Categories_CategoryId",
                table: "Words",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Translations_Languages_LanguageId",
                table: "Translations");

            migrationBuilder.DropForeignKey(
                name: "FK_Translations_Words_WordId",
                table: "Translations");

            migrationBuilder.DropForeignKey(
                name: "FK_Words_Categories_CategoryId",
                table: "Words");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Words",
                table: "Words");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Translations",
                table: "Translations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Languages",
                table: "Languages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Words",
                newName: "Word");

            migrationBuilder.RenameTable(
                name: "Translations",
                newName: "Translation");

            migrationBuilder.RenameTable(
                name: "Languages",
                newName: "Language");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "Category");

            migrationBuilder.RenameIndex(
                name: "IX_Words_CategoryId",
                table: "Word",
                newName: "IX_Word_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Translations_WordId_LanguageId",
                table: "Translation",
                newName: "IX_Translation_WordId_LanguageId");

            migrationBuilder.RenameIndex(
                name: "IX_Translations_LanguageId",
                table: "Translation",
                newName: "IX_Translation_LanguageId");

            migrationBuilder.RenameIndex(
                name: "IX_Languages_Code",
                table: "Language",
                newName: "IX_Language_Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Word",
                table: "Word",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Translation",
                table: "Translation",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Language",
                table: "Language",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Category",
                table: "Category",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Translation_Language_LanguageId",
                table: "Translation",
                column: "LanguageId",
                principalTable: "Language",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Translation_Word_WordId",
                table: "Translation",
                column: "WordId",
                principalTable: "Word",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Word_Category_CategoryId",
                table: "Word",
                column: "CategoryId",
                principalTable: "Category",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

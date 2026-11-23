using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class AddSetFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FolderId",
                table: "Sets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SetFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SetFolders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sets_FolderId",
                table: "Sets",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_SetFolders_UserId_Name",
                table: "SetFolders",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sets_SetFolders_FolderId",
                table: "Sets",
                column: "FolderId",
                principalTable: "SetFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sets_SetFolders_FolderId",
                table: "Sets");

            migrationBuilder.DropTable(
                name: "SetFolders");

            migrationBuilder.DropIndex(
                name: "IX_Sets_FolderId",
                table: "Sets");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Sets");
        }
    }
}

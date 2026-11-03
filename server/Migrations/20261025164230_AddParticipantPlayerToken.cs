using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuattroLingo.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantPlayerToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlayerTokenHash",
                table: "Participants",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerTokenHash",
                table: "Participants");
        }
    }
}

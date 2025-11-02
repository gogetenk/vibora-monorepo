using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vibora.Games.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNotesFromGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Games",
                type: "text",
                nullable: true);
        }
    }
}

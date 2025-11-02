using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vibora.Games.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestExternalIdToGuestParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestExternalId",
                table: "GuestParticipants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestExternalId",
                table: "GuestParticipants");
        }
    }
}

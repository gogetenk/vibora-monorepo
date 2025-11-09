using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vibora.Games.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index composite on Participations (GameId, IsHost) for faster host lookups
            // This helps when querying for the host of a specific game
            migrationBuilder.CreateIndex(
                name: "IX_Participations_GameId_IsHost",
                table: "Participations",
                columns: new[] { "GameId", "IsHost" });

            // Index composite on Games (Status, CurrentPlayers, MaxPlayers) for faster open games queries
            // This helps when filtering games that are open and have available spots
            migrationBuilder.CreateIndex(
                name: "IX_Games_Status_CurrentPlayers_MaxPlayers",
                table: "Games",
                columns: new[] { "Status", "CurrentPlayers", "MaxPlayers" });

            // Index on GuestParticipants.GuestExternalId for faster reconciliation queries
            // This helps when checking if a guest already joined a game
            migrationBuilder.CreateIndex(
                name: "IX_GuestParticipants_GuestExternalId",
                table: "GuestParticipants",
                column: "GuestExternalId",
                filter: "\"GuestExternalId\" IS NOT NULL");

            // Index composite on Participations (UserExternalId, GameId) to speed up duplicate checks
            // This helps when checking if a user already joined a specific game
            migrationBuilder.CreateIndex(
                name: "IX_Participations_UserExternalId_GameId",
                table: "Participations",
                columns: new[] { "UserExternalId", "GameId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Participations_GameId_IsHost",
                table: "Participations");

            migrationBuilder.DropIndex(
                name: "IX_Games_Status_CurrentPlayers_MaxPlayers",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_GuestParticipants_GuestExternalId",
                table: "GuestParticipants");

            migrationBuilder.DropIndex(
                name: "IX_Participations_UserExternalId_GameId",
                table: "Participations");
        }
    }
}

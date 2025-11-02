using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vibora.Games.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSharesAndGuestParticipantsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedByUserExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ShareToken = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameShares_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestParticipants_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameShares_GameId",
                table: "GameShares",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameShares_ShareToken",
                table: "GameShares",
                column: "ShareToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestParticipants_Email",
                table: "GuestParticipants",
                column: "Email",
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GuestParticipants_GameId",
                table: "GuestParticipants",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestParticipants_PhoneNumber",
                table: "GuestParticipants",
                column: "PhoneNumber",
                filter: "\"PhoneNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameShares");

            migrationBuilder.DropTable(
                name: "GuestParticipants");
        }
    }
}

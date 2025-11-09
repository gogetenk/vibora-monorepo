using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vibora.Users.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixSkillLevelType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserNotificationSettings_UserExternalId",
                table: "UserNotificationSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationSettings_UserExternalId",
                table: "UserNotificationSettings",
                column: "UserExternalId");
        }
    }
}

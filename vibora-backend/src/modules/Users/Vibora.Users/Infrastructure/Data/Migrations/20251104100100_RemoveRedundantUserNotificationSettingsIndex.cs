using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vibora.Users.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantUserNotificationSettingsIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove redundant index on UserExternalId since it's already the PRIMARY KEY
            // Primary keys are automatically indexed, so this index is redundant and wastes space
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

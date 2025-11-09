using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vibora.Notifications.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveUserNotificationPreferencesToNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create UserNotificationPreferences table in Notifications schema
            migrationBuilder.CreateTable(
                name: "UserNotificationPreferences",
                columns: table => new
                {
                    UserExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeviceToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PushEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationPreferences", x => x.UserExternalId);
                });

            // Migrate data from Users.UserNotificationSettings to Notifications.UserNotificationPreferences
            // Only if the source table exists (handles fresh installations)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_name = 'UserNotificationSettings'
                    ) THEN
                        INSERT INTO ""UserNotificationPreferences"" (
                            ""UserExternalId"",
                            ""DeviceToken"",
                            ""PhoneNumber"",
                            ""Email"",
                            ""PushEnabled"",
                            ""SmsEnabled"",
                            ""EmailEnabled"",
                            ""CreatedAt"",
                            ""UpdatedAt""
                        )
                        SELECT
                            ""UserExternalId"",
                            ""DeviceToken"",
                            ""PhoneNumber"",
                            ""Email"",
                            ""PushEnabled"",
                            ""SmsEnabled"",
                            ""EmailEnabled"",
                            ""CreatedAt"",
                            ""UpdatedAt""
                        FROM ""UserNotificationSettings""
                        ON CONFLICT (""UserExternalId"") DO NOTHING;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotificationPreferences");
        }
    }
}

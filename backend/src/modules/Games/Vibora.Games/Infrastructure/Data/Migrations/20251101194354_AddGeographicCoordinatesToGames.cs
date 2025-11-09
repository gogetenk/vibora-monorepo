using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vibora.Games.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeographicCoordinatesToGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable PostGIS extension (includes earthdistance/cube functionality)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");

            // Add latitude/longitude columns (keep for backward compatibility and data entry)
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Games",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Games",
                type: "double precision",
                nullable: true);

            // Add PostGIS geography column for efficient spatial queries
            migrationBuilder.Sql(@"
                ALTER TABLE ""Games""
                ADD COLUMN ""LocationGeog"" geography(Point,4326);
            ");

            // Create trigger to auto-populate LocationGeog from lat/lng
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_location_geog()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF NEW.""Latitude"" IS NOT NULL AND NEW.""Longitude"" IS NOT NULL THEN
                        NEW.""LocationGeog"" := ST_SetSRID(ST_MakePoint(NEW.""Longitude"", NEW.""Latitude""), 4326)::geography;
                    ELSE
                        NEW.""LocationGeog"" := NULL;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_update_location_geog
                BEFORE INSERT OR UPDATE OF ""Latitude"", ""Longitude""
                ON ""Games""
                FOR EACH ROW
                EXECUTE FUNCTION update_location_geog();
            ");

            // Create GiST index for spatial queries
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_Games_LocationGeog""
                ON ""Games"" USING GIST (""LocationGeog"")
                WHERE ""LocationGeog"" IS NOT NULL;
            ");

            // Note: Composite index (Status, DateTime) already exists from previous migrations
            // No need for partial index with NOW() (would be rejected as NOW() is not IMMUTABLE)

            // Add comments
            migrationBuilder.Sql(@"
                COMMENT ON COLUMN ""Games"".""Latitude"" IS 'GPS latitude (for data entry and display)';
                COMMENT ON COLUMN ""Games"".""Longitude"" IS 'GPS longitude (for data entry and display)';
                COMMENT ON COLUMN ""Games"".""LocationGeog"" IS 'PostGIS geography point for efficient spatial queries (auto-populated from lat/lng)';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_update_location_geog ON \"Games\";");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_location_geog();");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Games_LocationGeog\";");
            migrationBuilder.Sql("ALTER TABLE \"Games\" DROP COLUMN IF EXISTS \"LocationGeog\";");
            migrationBuilder.DropColumn(name: "Latitude", table: "Games");
            migrationBuilder.DropColumn(name: "Longitude", table: "Games");
        }
    }
}

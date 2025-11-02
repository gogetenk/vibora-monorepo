var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database with PostGIS extension and persistent volume
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis") // PostGIS-enabled Docker image
    .WithDataVolume("vibora-postgres-data") // Named volume for data persistence
    .WithPgAdmin()
    .WithIconName("database");

var viboraDb = postgres.AddDatabase("viboradb")
    .WithIconName("memory");

// Migration service - runs first to apply EF Core migrations
var migrations = builder.AddProject<Projects.Vibora_MigrationService>("vibora-migrations")
    .WithReference(viboraDb)
    .WaitFor(viboraDb)
    .WithIconName("airplane take off");

// API Web (Gateway) - waits for migrations to complete
var apiService = builder.AddProject<Projects.Vibora_Web>("vibora-web")
    .WithReference(viboraDb)
    .WithReference(migrations)
    .WaitForCompletion(migrations);

builder.AddDockerComposeEnvironment("dev");
builder.Build().Run();

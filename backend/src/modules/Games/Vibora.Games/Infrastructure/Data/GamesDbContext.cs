using Microsoft.EntityFrameworkCore;
using Vibora.Games.Domain;

namespace Vibora.Games.Infrastructure.Data;

public sealed class GamesDbContext : DbContext
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Participation> Participations => Set<Participation>();
    public DbSet<GameShare> GameShares => Set<GameShare>();
    public DbSet<GuestParticipant> GuestParticipants => Set<GuestParticipant>();

    public GamesDbContext(DbContextOptions<GamesDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure PostGIS extension
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("Games");
            entity.HasKey(g => g.Id);

            entity.Property(g => g.DateTime)
                .IsRequired();

            entity.Property(g => g.Location)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(g => g.SkillLevel)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(g => g.MaxPlayers)
                .IsRequired();

            entity.Property(g => g.CurrentPlayers)
                .IsRequired();

            entity.Property(g => g.HostExternalId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(g => g.Status)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(g => g.CreatedAt)
                .IsRequired();

            entity.Property(g => g.Latitude)
                .HasColumnType("double precision")
                .IsRequired(false);

            entity.Property(g => g.Longitude)
                .HasColumnType("double precision")
                .IsRequired(false);

            // Configure PostGIS geography column
            entity.Property(g => g.LocationGeog)
                .HasColumnType("geography(Point,4326)")
                .IsRequired(false);

            // Index for finding open games
            entity.HasIndex(g => new { g.Status, g.DateTime });

            // Relationship with Participations
            entity.HasMany(g => g.Participations)
                .WithOne()
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with GuestParticipants
            entity.HasMany(g => g.GuestParticipants)
                .WithOne(gp => gp.Game)
                .HasForeignKey(gp => gp.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Participation>(entity =>
        {
            entity.ToTable("Participations");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.GameId)
                .IsRequired();

            entity.Property(p => p.UserExternalId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(p => p.UserName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.UserSkillLevel)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.IsHost)
                .IsRequired();

            entity.Property(p => p.JoinedAt)
                .IsRequired();

            // Index for finding user's games
            entity.HasIndex(p => p.UserExternalId);

            // Index for finding game participants
            entity.HasIndex(p => p.GameId);
        });

        modelBuilder.Entity<GameShare>(entity =>
        {
            entity.ToTable("GameShares");
            entity.HasKey(gs => gs.Id);

            entity.Property(gs => gs.GameId)
                .IsRequired();

            entity.Property(gs => gs.SharedByUserExternalId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(gs => gs.ShareToken)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(gs => gs.ViewCount)
                .IsRequired();

            entity.Property(gs => gs.CreatedAt)
                .IsRequired();

            entity.Property(gs => gs.ExpiresAt);

            // Unique index on ShareToken for fast lookups
            entity.HasIndex(gs => gs.ShareToken)
                .IsUnique();

            // Index for finding shares by game
            entity.HasIndex(gs => gs.GameId);

            // Relationship with Game
            entity.HasOne(gs => gs.Game)
                .WithMany()
                .HasForeignKey(gs => gs.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GuestParticipant>(entity =>
        {
            entity.ToTable("GuestParticipants");
            entity.HasKey(gp => gp.Id);

            entity.Property(gp => gp.GameId)
                .IsRequired();

            entity.Property(gp => gp.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(gp => gp.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(gp => gp.Email)
                .HasMaxLength(255);

            entity.Property(gp => gp.JoinedAt)
                .IsRequired();

            // Index for finding guests by game
            entity.HasIndex(gp => gp.GameId);

            // Index for finding guests by phone number (for reconciliation)
            entity.HasIndex(gp => gp.PhoneNumber)
                .HasFilter("\"PhoneNumber\" IS NOT NULL");

            // Index for finding guests by email (for reconciliation)
            entity.HasIndex(gp => gp.Email)
                .HasFilter("\"Email\" IS NOT NULL");
        });
    }
}

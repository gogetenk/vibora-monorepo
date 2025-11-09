using Microsoft.EntityFrameworkCore;
using Vibora.Users.Domain;

namespace Vibora.Users.Infrastructure.Data;

public sealed class UsersDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            
            // ExternalId is now the PRIMARY KEY
            entity.HasKey(u => u.ExternalId);
            
            entity.Property(u => u.ExternalId)
                .IsRequired()
                .HasMaxLength(255);

            // Legacy field for backward compatibility
            entity.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            // New profile fields
            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.LastName)
                .HasMaxLength(50);
            
            entity.Property(u => u.SkillLevel)
                .IsRequired();

            entity.Property(u => u.Bio)
                .HasMaxLength(500);

            entity.Property(u => u.PhotoUrl)
                .HasMaxLength(500);

            entity.Property(u => u.IsGuest)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.UpdatedAt)
                .IsRequired();

            entity.Property(u => u.LastSyncedAt);

            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(u => u.Email)
                .HasMaxLength(255);

            // Indexes for matching performance (phone/email based lookups)
            entity.HasIndex(u => u.PhoneNumber)
                .HasDatabaseName("IX_Users_PhoneNumber")
                .HasFilter("\"PhoneNumber\" IS NOT NULL");

            entity.HasIndex(u => u.Email)
                .HasDatabaseName("IX_Users_Email")
                .HasFilter("\"Email\" IS NOT NULL");

            // Ignore domain events (not persisted)
            entity.Ignore(u => u.DomainEvents);
        });
    }
}

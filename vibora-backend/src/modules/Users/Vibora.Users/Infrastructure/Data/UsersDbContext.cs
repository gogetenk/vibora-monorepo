using Microsoft.EntityFrameworkCore;
using Vibora.Users.Domain;

namespace Vibora.Users.Infrastructure.Data;

public sealed class UsersDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserNotificationSettings> UserNotificationSettings => Set<UserNotificationSettings>();

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

        modelBuilder.Entity<UserNotificationSettings>(entity =>
        {
            entity.ToTable("UserNotificationSettings");
            
            // UserExternalId is the PRIMARY KEY
            entity.HasKey(s => s.UserExternalId);
            
            entity.Property(s => s.UserExternalId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(s => s.DeviceToken)
                .HasMaxLength(500);

            entity.Property(s => s.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(s => s.Email)
                .HasMaxLength(255);

            entity.Property(s => s.PushEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(s => s.SmsEnabled)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(s => s.EmailEnabled)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(s => s.CreatedAt)
                .IsRequired();

            entity.Property(s => s.UpdatedAt)
                .IsRequired();

            // Note: UserExternalId is the PRIMARY KEY, so no additional index needed
            // Ignore domain events (not persisted)
            entity.Ignore(s => s.DomainEvents);
        });
    }
}

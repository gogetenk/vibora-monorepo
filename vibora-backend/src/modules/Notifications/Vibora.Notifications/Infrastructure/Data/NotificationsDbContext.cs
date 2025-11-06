using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Infrastructure.Data;

/// <summary>
/// DbContext for the Notifications module
/// Manages Notification aggregate and UserNotificationPreferences
/// </summary>
public sealed class NotificationsDbContext : DbContext
{
    public DbSet<Notification> Notifications => Set<Notification>();
    internal DbSet<UserNotificationPreferences> UserNotificationPreferences => Set<UserNotificationPreferences>();

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            // Primary Key
            entity.HasKey(n => n.NotificationId);

            // Properties
            entity.Property(n => n.NotificationId)
                .IsRequired()
                .ValueGeneratedNever(); // Set manually in domain

            entity.Property(n => n.UserId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(n => n.Type)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(n => n.Channel)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(n => n.Status)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(n => n.CreatedAt)
                .IsRequired();

            entity.Property(n => n.SentAt);

            entity.Property(n => n.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(n => n.ErrorMessage)
                .HasMaxLength(1000);

            entity.Property(n => n.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(n => n.DeletedAt);

            // NotificationContent as Owned Entity (Value Object)
            entity.OwnsOne(n => n.Content, content =>
            {
                content.Property(c => c.Title)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnName("ContentTitle");

                content.Property(c => c.Body)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .HasColumnName("ContentBody");

                // Store Data as JSON
                content.Property(c => c.Data)
                    .HasColumnType("jsonb")
                    .HasColumnName("ContentData")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null));
            });

            // Indexes for common queries
            entity.HasIndex(n => n.UserId)
                .HasDatabaseName("IX_Notifications_UserId");

            entity.HasIndex(n => n.Status)
                .HasDatabaseName("IX_Notifications_Status");

            entity.HasIndex(n => new { n.UserId, n.Status })
                .HasDatabaseName("IX_Notifications_UserId_Status");

            entity.HasIndex(n => new { n.Status, n.CreatedAt })
                .HasDatabaseName("IX_Notifications_Status_CreatedAt");

            entity.HasIndex(n => new { n.UserId, n.DeletedAt })
                .HasDatabaseName("IX_Notifications_UserId_DeletedAt");

            // Ignore domain events (not persisted)
            entity.Ignore(n => n.DomainEvents);
        });

        modelBuilder.Entity<UserNotificationPreferences>(entity =>
        {
            entity.ToTable("UserNotificationPreferences");

            // UserExternalId is the PRIMARY KEY
            entity.HasKey(p => p.UserExternalId);

            entity.Property(p => p.UserExternalId)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(p => p.DeviceToken)
                .HasMaxLength(500);

            entity.Property(p => p.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(p => p.Email)
                .HasMaxLength(255);

            entity.Property(p => p.PushEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(p => p.SmsEnabled)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(p => p.EmailEnabled)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .IsRequired();

            // Ignore domain events (not persisted)
            entity.Ignore(p => p.DomainEvents);
        });
    }
}

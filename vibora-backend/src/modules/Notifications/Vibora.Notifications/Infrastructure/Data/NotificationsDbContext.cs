using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vibora.Notifications.Domain;

namespace Vibora.Notifications.Infrastructure.Data;

/// <summary>
/// DbContext for the Notifications module
/// Manages Notification aggregate and its entities
/// </summary>
public sealed class NotificationsDbContext : DbContext
{
    public DbSet<Notification> Notifications => Set<Notification>();

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
    }
}

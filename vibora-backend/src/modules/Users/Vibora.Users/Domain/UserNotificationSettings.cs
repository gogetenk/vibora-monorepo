using Ardalis.Result;
using Vibora.Shared.Domain;

namespace Vibora.Users.Domain;

/// <summary>
/// Stores user's notification preferences and contact information
/// Owned by User - one-to-one relationship
/// </summary>
public sealed class UserNotificationSettings : AggregateRoot
{
    public string UserExternalId { get; private set; } = string.Empty;

    // Contact Information for notifications
    public string? DeviceToken { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }

    // Channel Preferences
    public bool PushEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    private UserNotificationSettings() { }

    /// <summary>
    /// Create default notification settings for a new user
    /// </summary>
    public static UserNotificationSettings CreateDefault(string userExternalId, string? email = null)
    {
        return new UserNotificationSettings
        {
            UserExternalId = userExternalId,
            Email = email,
            PushEnabled = true,  // Enable push by default
            SmsEnabled = false,
            EmailEnabled = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Update channel preferences
    /// </summary>
    public Result UpdatePreferences(bool pushEnabled, bool smsEnabled, bool emailEnabled)
    {
        PushEnabled = pushEnabled;
        SmsEnabled = smsEnabled;
        EmailEnabled = emailEnabled;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Update device token for push notifications
    /// </summary>
    public Result UpdateDeviceToken(string deviceToken)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            errors.Add(new ValidationError(nameof(deviceToken), "Device token cannot be empty"));
        }
        else if (deviceToken.Length > 500)
        {
            errors.Add(new ValidationError(nameof(deviceToken), "Device token must not exceed 500 characters"));
        }

        if (errors.Any())
        {
            return Result.Invalid(errors);
        }

        DeviceToken = deviceToken;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Update phone number for SMS notifications
    /// </summary>
    public Result UpdatePhoneNumber(string phoneNumber)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            errors.Add(new ValidationError(nameof(phoneNumber), "Phone number cannot be empty"));
        }
        else if (phoneNumber.Length > 20)
        {
            errors.Add(new ValidationError(nameof(phoneNumber), "Phone number must not exceed 20 characters"));
        }

        if (errors.Any())
        {
            return Result.Invalid(errors);
        }

        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Update email for email notifications
    /// </summary>
    public Result UpdateEmail(string email)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(new ValidationError(nameof(email), "Email cannot be empty"));
        }
        else if (email.Length > 255)
        {
            errors.Add(new ValidationError(nameof(email), "Email must not exceed 255 characters"));
        }
        else if (!email.Contains('@'))
        {
            errors.Add(new ValidationError(nameof(email), "Email must be a valid email address"));
        }

        if (errors.Any())
        {
            return Result.Invalid(errors);
        }

        Email = email.Trim().ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Check if a specific channel is enabled and has contact info
    /// </summary>
    public bool CanReceiveNotification(string channel)
    {
        return channel.ToLower() switch
        {
            "push" => PushEnabled && !string.IsNullOrWhiteSpace(DeviceToken),
            "sms" => SmsEnabled && !string.IsNullOrWhiteSpace(PhoneNumber),
            "email" => EmailEnabled && !string.IsNullOrWhiteSpace(Email),
            _ => false
        };
    }

    /// <summary>
    /// Get the recipient identifier for a specific channel
    /// </summary>
    public string? GetRecipient(string channel)
    {
        return channel.ToLower() switch
        {
            "push" => DeviceToken,
            "sms" => PhoneNumber,
            "email" => Email,
            _ => null
        };
    }
}

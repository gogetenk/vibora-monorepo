using Microsoft.EntityFrameworkCore;
using Vibora.Users.Domain;
using Vibora.Users.Infrastructure.Data;

namespace Vibora.Users.Infrastructure.Persistence;

/// <summary>
/// Repository implementation for UserNotificationSettings aggregate
/// </summary>
internal sealed class UserNotificationSettingsRepository : IUserNotificationSettingsRepository
{
    private readonly UsersDbContext _context;

    public UserNotificationSettingsRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task<UserNotificationSettings?> GetByUserExternalIdAsync(string userExternalId, CancellationToken cancellationToken = default)
    {
        return await _context.UserNotificationSettings
            .FirstOrDefaultAsync(s => s.UserExternalId == userExternalId, cancellationToken);
    }

    public void Add(UserNotificationSettings settings)
    {
        _context.UserNotificationSettings.Add(settings);
    }

    public void Update(UserNotificationSettings settings)
    {
        _context.UserNotificationSettings.Update(settings);
    }

    public void Remove(UserNotificationSettings settings)
    {
        _context.UserNotificationSettings.Remove(settings);
    }
}

using Microsoft.EntityFrameworkCore;
using Vibora.Users.Domain;
using Vibora.Users.Infrastructure.Data;

namespace Vibora.Users.Infrastructure.Persistence;

internal sealed class UserRepository : IUserRepository
{
    private readonly UsersDbContext _dbContext;

    public UserRepository(UsersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);
    }

    public async Task<User?> GetNonGuestByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.ExternalId == externalId && !u.IsGuest,
                cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.ExternalId == externalId, cancellationToken);
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }

    public void Update(User user)
    {
        _dbContext.Users.Update(user);
    }

    public void Remove(User user)
    {
        _dbContext.Users.Remove(user);
    }

    public async Task<User?> GetGuestByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.IsGuest && u.PhoneNumber == phoneNumber,
                cancellationToken);
    }

    public async Task<User?> GetGuestByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.Users
            .FirstOrDefaultAsync(
                u => u.IsGuest && u.Email == normalizedEmail,
                cancellationToken);
    }
}

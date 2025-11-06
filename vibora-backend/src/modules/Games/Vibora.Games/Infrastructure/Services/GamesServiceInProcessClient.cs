using MediatR;
using Vibora.Games.Contracts.Services;

namespace Vibora.Games.Infrastructure.Services;

/// <summary>
/// In-process client to query Games module (Monolith mode)
/// Calls Games module queries via MediatR (same process)
/// PUBLIC class - can be used by other modules
/// </summary>
public sealed class GamesServiceInProcessClient : IGamesServiceClient
{
    private readonly ISender _sender;

    public GamesServiceInProcessClient(ISender sender)
    {
        _sender = sender;
    }

    public async Task<int> GetUserGamesCountAsync(
        string userExternalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new Application.Queries.GetUserGamesCount.GetUserGamesCountQuery(userExternalId);
            var result = await _sender.Send(query, cancellationToken);

            return result.IsSuccess ? result.Value : 0;
        }
        catch
        {
            // Graceful degradation: return 0 if Games module unavailable
            return 0;
        }
    }

    public async Task<List<GuestParticipationDto>> GetGuestParticipationsByContactAsync(
        string? phoneNumber,
        string? email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new Application.Queries.GetGuestParticipationsByContact
                .GetGuestParticipationsByContactQuery(phoneNumber, email);
            var result = await _sender.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return new List<GuestParticipationDto>();
            }

            // Map internal DTOs to public contract DTOs
            return result.Value.GuestParticipations
                .Select(gp => new GuestParticipationDto(
                    gp.GuestParticipantId,
                    gp.GameId,
                    gp.Name,
                    gp.PhoneNumber,
                    gp.Email,
                    gp.JoinedAt))
                .ToList();
        }
        catch
        {
            // Graceful degradation: return empty list if Games module unavailable
            return new List<GuestParticipationDto>();
        }
    }

    public async Task<int> ConvertGuestParticipationsAsync(
        List<Guid> guestParticipantIds,
        string userExternalId,
        string userName,
        string userSkillLevel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new Application.Commands.ConvertGuestParticipations
                .ConvertGuestParticipationsCommand(
                    guestParticipantIds,
                    userExternalId,
                    userName,
                    userSkillLevel);

            var result = await _sender.Send(command, cancellationToken);

            return result.IsSuccess ? result.Value.ConvertedCount : 0;
        }
        catch
        {
            // Graceful degradation: return 0 if Games module unavailable
            return 0;
        }
    }

    public async Task<List<string>> GetGameParticipantIdsAsync(
        Guid gameId,
        string? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new Application.Queries.GetGameParticipantIds.GetGameParticipantIdsQuery(gameId, excludeUserId);
            var result = await _sender.Send(query, cancellationToken);

            return result.IsSuccess ? result.Value : new List<string>();
        }
        catch
        {
            // Graceful degradation: return empty list if Games module unavailable
            return new List<string>();
        }
    }
}

using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.GetGameParticipantIds;

/// <summary>
/// Query to get all participant user IDs for a specific game
/// Used by Notifications module to notify all participants when someone joins
/// </summary>
internal sealed record GetGameParticipantIdsQuery(
    Guid GameId,
    string? ExcludeUserId = null
) : IRequest<Result<List<string>>>;

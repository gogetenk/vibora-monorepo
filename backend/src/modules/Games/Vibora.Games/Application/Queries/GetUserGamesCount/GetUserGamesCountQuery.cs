using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.GetUserGamesCount;

/// <summary>
/// Query to get the count of games a user has participated in
/// Used by other modules via IGamesServiceClient
/// </summary>
internal sealed record GetUserGamesCountQuery(
    string UserExternalId
) : IRequest<Result<int>>;

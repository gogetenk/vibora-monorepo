using Ardalis.Result;
using MediatR;

namespace Vibora.Games.Application.Queries.GetShareMetadata;

/// <summary>
/// Query to get Open Graph metadata for a shared game (for WhatsApp/Telegram preview)
/// </summary>
internal sealed record GetShareMetadataQuery(
    string ShareToken
) : IRequest<Result<GetShareMetadataResult>>;

/// <summary>
/// Result containing Open Graph metadata for social media previews
/// </summary>
internal sealed record GetShareMetadataResult(
    string Title,
    string Description,
    string Location,
    DateTime GameDateTime,
    string SkillLevel,
    int CurrentPlayers,
    int MaxPlayers,
    string GameStatus
);

using Ardalis.Result;
using MediatR;
using Vibora.Games.Domain;

namespace Vibora.Games.Application.Queries.GetAvailableGames;

/// <summary>
/// Handler for retrieving available games with optional filtering
/// Uses repository for data access (Clean Architecture)
/// </summary>
internal sealed class GetAvailableGamesQueryHandler
    : IRequestHandler<GetAvailableGamesQuery, Result<GetAvailableGamesResult>>
{
    private readonly IGameRepository _gameRepository;

    public GetAvailableGamesQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<GetAvailableGamesResult>> Handle(
        GetAvailableGamesQuery request,
        CancellationToken cancellationToken)
    {
        // Collect all validation errors before returning
        var validationErrors = new List<ValidationError>();

        // Validate pagination parameters
        if (request.PageNumber < 1)
        {
            validationErrors.Add(new ValidationError("PageNumber must be greater than 0"));
        }

        if (request.PageSize < 1 || request.PageSize > 100)
        {
            validationErrors.Add(new ValidationError("PageSize must be between 1 and 100"));
        }

        // Validate date range
        if (request.FromDate.HasValue && request.ToDate.HasValue 
            && request.FromDate.Value > request.ToDate.Value)
        {
            validationErrors.Add(new ValidationError("FromDate must be before ToDate"));
        }

        // Return all validation errors at once
        if (validationErrors.Any())
        {
            return Result.Invalid(validationErrors);
        }

        // Query via repository (Infrastructure layer)
        var (games, totalCount) = await _gameRepository.GetAvailableGamesAsync(
            request.Location,
            request.SkillLevel,
            request.FromDate,
            request.ToDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken
        );

        // Map domain entities to DTOs
        var gameDtos = games.Select(g => new GameListItemDto(
            g.Id,
            g.DateTime,
            g.Location,
            g.SkillLevel,
            g.MaxPlayers,
            g.CurrentPlayers,
            g.HostExternalId,
            g.Status.ToString(),
            g.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var result = new GetAvailableGamesResult(
            Items: gameDtos,
            TotalCount: totalCount,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            TotalPages: totalPages
        );

        return Result.Success(result);
    }
}

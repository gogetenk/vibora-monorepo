using Ardalis.Result;
using MediatR;
using Vibora.Games.Contracts.Services;
using Vibora.Users.Application.DTOs;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Queries.GetUserPublicProfile;

/// <summary>
/// Handler for retrieving another user's public profile
/// Privacy rules: LastName is hidden (only first letter shown)
/// Cross-module communication: Uses IGamesServiceClient to query Games module
/// </summary>
internal sealed class GetUserPublicProfileQueryHandler
    : IRequestHandler<GetUserPublicProfileQuery, Result<UserPublicProfileDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IGamesServiceClient _gamesClient;

    public GetUserPublicProfileQueryHandler(
        IUserRepository userRepository,
        IGamesServiceClient gamesClient)
    {
        _userRepository = userRepository;
        _gamesClient = gamesClient;
    }

    public async Task<Result<UserPublicProfileDto>> Handle(
        GetUserPublicProfileQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Get user
        var user = await _userRepository.GetByExternalIdAsync(
            request.TargetUserExternalId,
            cancellationToken);

        if (user == null)
        {
            return Result<UserPublicProfileDto>.NotFound("User not found");
        }

        // 2. Count games played via Games module service client (cross-module query)
        // IGamesServiceClient abstracts monolith vs microservices communication
        var gamesCount = await _gamesClient.GetUserGamesCountAsync(
            request.TargetUserExternalId,
            cancellationToken);

        // 3. Map to public DTO with privacy rules
        // Hide full LastName - only show first letter + "."
        var lastNameInitial = !string.IsNullOrWhiteSpace(user.LastName)
            ? $"{user.LastName[0]}."
            : null;

        var dto = new UserPublicProfileDto(
            user.FirstName,
            lastNameInitial,
            user.SkillLevel.ToString(),
            user.Bio,
            user.PhotoUrl,
            gamesCount,
            user.CreatedAt
        );

        return Result<UserPublicProfileDto>.Success(dto);
    }
}

using Ardalis.Result;
using MediatR;
using Vibora.Games.Contracts.Services;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Queries.GetCurrentUserProfile;

/// <summary>
/// Handler for retrieving the authenticated user's full profile including games statistics
/// Cross-module communication: Uses IGamesServiceClient to query Games module
/// </summary>
internal sealed class GetCurrentUserProfileQueryHandler
    : IRequestHandler<GetCurrentUserProfileQuery, Result<UserProfileDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IGamesServiceClient _gamesClient;

    public GetCurrentUserProfileQueryHandler(
        IUserRepository userRepository,
        IGamesServiceClient gamesClient)
    {
        _userRepository = userRepository;
        _gamesClient = gamesClient;
    }

    public async Task<Result<UserProfileDto>> Handle(
        GetCurrentUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Get user by ExternalId
        var user = await _userRepository.GetByExternalIdAsync(
            request.UserExternalId,
            cancellationToken);

        if (user == null)
        {
            return Result<UserProfileDto>.NotFound("User not found");
        }

        // 2. Count games played via Games module service client (cross-module query)
        // IGamesServiceClient abstracts monolith vs microservices communication
        var gamesCount = await _gamesClient.GetUserGamesCountAsync(
            request.UserExternalId,
            cancellationToken);

        // 3. Map to DTO
        var dto = new UserProfileDto(
            ExternalId: user.ExternalId,
            FirstName: user.FirstName,
            LastName: user.LastName,
            SkillLevel: user.SkillLevel.ToString(),
            Bio: user.Bio,
            PhotoUrl: user.PhotoUrl,
            GamesPlayedCount: gamesCount,
            MemberSince: user.CreatedAt
        );

        return Result<UserProfileDto>.Success(dto);
    }
}

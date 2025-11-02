using Ardalis.Result;
using MediatR;
using Vibora.Users.Contracts.Queries;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Queries.GetUserByExternalId;

/// <summary>
/// Public handler for cross-module queries
/// Can be called from other modules via MediatR (monolith mode)
/// </summary>
internal sealed class GetUserByExternalIdQueryHandler
    : IRequestHandler<GetUserByExternalIdQuery, Result<UserMetadataResult>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByExternalIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserMetadataResult>> Handle(
        GetUserByExternalIdQuery request,
        CancellationToken cancellationToken)
    {
        // Use GetByExternalIdAsync to include guest users
        var user = await _userRepository.GetByExternalIdAsync(
            request.ExternalId,
            cancellationToken);

        if (user == null)
        {
            return Result<UserMetadataResult>.NotFound(
                $"User with ExternalId '{request.ExternalId}' not found");
        }

        return Result<UserMetadataResult>.Success(new UserMetadataResult(
            user.ExternalId,
            user.Name,
            user.SkillLevel
        ));
    }
}

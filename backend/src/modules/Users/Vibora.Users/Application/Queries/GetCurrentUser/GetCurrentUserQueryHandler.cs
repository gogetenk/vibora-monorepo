using Ardalis.Result;
using MediatR;
using Vibora.Shared.Extensions;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Queries.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler
    : IRequestHandler<GetCurrentUserQuery, Result<GetCurrentUserResult>>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetCurrentUserResult>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        // Find user by ExternalId (from JWT sub claim)
        var user = await _userRepository.GetNonGuestByExternalIdAsync(
            request.ExternalId,
            cancellationToken);

        if (user == null)
        {
            return Result<GetCurrentUserResult>.NotFound(
                "User not found. Please sync from auth provider first.");
        }

        return Result<GetCurrentUserResult>.Success(new GetCurrentUserResult(
            user.ExternalId,
            user.Name,
            user.SkillLevel.ToDisplayString(),
            user.Bio
        ));
    }
}

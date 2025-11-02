using Ardalis.Result;
using MediatR;
using Vibora.Users.Domain;

namespace Vibora.Users.Application.Queries.GetUserById;

internal sealed class GetUserByIdQueryHandler
    : IRequestHandler<GetUserByIdQuery, Result<GetUserByIdResult>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetUserByIdResult>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(
            request.ExternalId,
            cancellationToken);

        if (user == null)
        {
            return Result<GetUserByIdResult>.NotFound("User not found");
        }

        return Result<GetUserByIdResult>.Success(new GetUserByIdResult(
            user.ExternalId,
            user.Name,
            user.SkillLevel.ToString(),
            user.Bio
        ));
    }
}

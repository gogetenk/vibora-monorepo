using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Application.Commands.UploadUserProfilePhoto;

internal sealed record UploadUserProfilePhotoCommand(
    string ExternalId, // From JWT
    Stream PhotoStream,
    string ContentType
) : IRequest<Result<UploadUserProfilePhotoResult>>;

internal sealed record UploadUserProfilePhotoResult(
    string PhotoUrl
);

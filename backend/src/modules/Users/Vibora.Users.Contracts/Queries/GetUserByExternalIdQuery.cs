using Ardalis.Result;
using MediatR;

namespace Vibora.Users.Contracts.Queries;

/// <summary>
/// Query to get user metadata by ExternalId
/// Exposed publicly for cross-module communication (in-process calls)
/// </summary>
public sealed record GetUserByExternalIdQuery(string ExternalId) 
    : IRequest<Result<UserMetadataResult>>;

/// <summary>
/// Result DTO for GetUserByExternalIdQuery
/// </summary>
public sealed record UserMetadataResult(
    string ExternalId,
    string Name,
    int SkillLevel // 1-10 scale
);

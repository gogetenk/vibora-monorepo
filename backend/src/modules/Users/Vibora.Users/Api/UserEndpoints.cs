using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Vibora.Shared.Infrastructure.Caching;
using Vibora.Users.Application.Commands.ClaimGuestParticipations;
using Vibora.Users.Application.Commands.CreateGuestUser;
using Vibora.Users.Application.Commands.SyncUserFromAuth;
using Vibora.Users.Application.Commands.UpdateUserProfile;
using Vibora.Users.Application.Commands.UploadUserProfilePhoto;
using Vibora.Users.Application.DTOs;
using Vibora.Users.Application.Queries.GetCurrentUser;
using Vibora.Users.Application.Queries.GetCurrentUserProfile;
using Vibora.Users.Application.Queries.GetUserById;
using Vibora.Users.Application.Queries.GetUserPublicProfile;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Vibora.Users.Api;

internal static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var usersGroup = endpoints.MapGroup("/users")
            .WithTags("Users");

        // Webhook from Auth0/Supabase to sync user metadata
        usersGroup.MapPost("/sync", SyncUserFromAuth)
            .WithName("SyncUserFromAuth")
            .AllowAnonymous() // Protected by webhook secret in production
            .Produces<SyncUserFromAuthResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        usersGroup.MapPost("/guest", CreateGuestUser)
            .WithName("CreateGuestUser")
            .AllowAnonymous()
            .Produces<CreateGuestUserResult>(StatusCodes.Status200OK);

        usersGroup.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .CacheOutput(policy => policy
                .SetVaryByHeader("Authorization") // Isolate cache per JWT token
                .Expire(TimeSpan.FromMinutes(5))
                .VaryByValue(context =>
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    return new KeyValuePair<string, string>("user", userId ?? "anonymous");
                }))
            .Produces<GetCurrentUserResult>(StatusCodes.Status200OK);

        usersGroup.MapPut("/me", UpdateProfile)
            .WithName("UpdateProfile")
            .RequireAuthorization()
            .Produces<UpdateUserProfileResult>(StatusCodes.Status200OK);

        // Profile endpoints
        usersGroup.MapGet("/profile", GetCurrentUserProfile)
            .WithName("GetCurrentUserProfile")
            .RequireAuthorization()
            .CacheOutput(policy => policy
                .SetVaryByHeader("Authorization") // Isolate cache per JWT token
                .Expire(TimeSpan.FromMinutes(5))
                .VaryByValue(context =>
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    return new KeyValuePair<string, string>("user", userId ?? "anonymous");
                }))
            .Produces<UserProfileDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        usersGroup.MapPut("/profile", UpdateUserProfileWithPhoto)
            .WithName("UpdateUserProfileWithPhoto")
            .RequireAuthorization()
            .DisableAntiforgery() // Required for file uploads
            .Produces<UserProfileDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        usersGroup.MapGet("/{externalId}/profile", GetUserPublicProfile)
            .WithName("GetUserPublicProfile")
            .AllowAnonymous() // Public profiles don't require authentication
            .CacheOutput(policy => policy
                .Expire(TimeSpan.FromMinutes(5))
                .VaryByValue(context =>
                {
                    var userId = context.Request.RouteValues["externalId"]?.ToString();
                    return new KeyValuePair<string, string>("externalId", userId ?? "unknown");
                }))
            .Produces<UserPublicProfileDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        usersGroup.MapGet("/{externalId}", GetUserById)
            .WithName("GetUserById")
            .RequireAuthorization()
            .Produces<GetUserByIdResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        usersGroup.MapPost("/claim-guest-participations", ClaimGuestParticipations)
            .WithName("ClaimGuestParticipations")
            .RequireAuthorization()
            .Produces<ClaimGuestParticipationsResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return endpoints;
    }

    // POST /users/sync - Webhook from Auth0/Supabase
    private static async Task<HttpResult> SyncUserFromAuth(
        SyncUserFromAuthRequest request,
        ISender sender)
    {
        // TODO: Validate webhook signature/secret in production

        var command = new SyncUserFromAuthCommand(
            request.ExternalId,
            request.Name,
            request.SkillLevel ?? Domain.SkillLevelConstants.Default, // Default: 5 (Intermediate)
            request.FirstName,  // NEW
            request.LastName,   // NEW
            request.PhoneNumber,
            request.Email);

        var result = await sender.Send(command);
        return result.ToMinimalApiResult();
    }

    // POST /users/guest - Create guest user
    private static async Task<HttpResult> CreateGuestUser(
        CreateGuestUserRequest request,
        ISender sender)
    {
        var command = new CreateGuestUserCommand(
            request.Name,
            request.SkillLevel,
            request.PhoneNumber,
            request.Email);

        var result = await sender.Send(command);
        return result.ToMinimalApiResult();
    }

    // GET /users/me - Get current user profile
    private static async Task<HttpResult> GetCurrentUser(
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var query = new GetCurrentUserQuery(externalId);
        var result = await sender.Send(query);
        return result.ToMinimalApiResult();
    }

    // PUT /users/me - Update profile
    private static async Task<HttpResult> UpdateProfile(
        UpdateUserProfileRequest request,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        // Parse Name into FirstName/LastName for backward compatibility
        var nameParts = request.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : request.Name;
        var lastName = nameParts.Length > 1 ? nameParts[1] : null;
        
        var command = new UpdateUserProfileCommand(
            externalId,
            firstName,
            lastName,
            request.SkillLevel,
            request.Bio);

        var result = await sender.Send(command);
        return result.ToMinimalApiResult();
    }

    // GET /users/me/profile - Get current user's full profile with statistics
    private static async Task<HttpResult> GetCurrentUserProfile(
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var query = new GetCurrentUserProfileQuery(externalId);
        var result = await sender.Send(query);
        return result.ToMinimalApiResult();
    }

    // GET /users/{externalId}/profile - Get user's public profile
    private static async Task<HttpResult> GetUserPublicProfile(
        string externalId,
        ISender sender)
    {
        var query = new GetUserPublicProfileQuery(externalId);
        var result = await sender.Send(query);
        return result.ToMinimalApiResult();
    }

    // PUT /users/profile - Update profile with optional photo
    private static async Task<HttpResult> UpdateUserProfileWithPhoto(
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var form = await httpContext.Request.ReadFormAsync();

        // Extract profile fields
        var firstName = form["FirstName"].ToString();
        var lastName = form["LastName"].ToString();
        var skillLevelStr = form["SkillLevel"].ToString();
        var bio = form["Bio"].ToString();

        // Parse SkillLevel (1-10 scale)
        if (!int.TryParse(skillLevelStr, out var skillLevel))
        {
            return Results.UnprocessableEntity(new { error = "Invalid SkillLevel. Must be a number between 1 and 10." });
        }

        // Update profile (validation done in command handler)
        var updateCommand = new UpdateUserProfileCommand(
            externalId,
            firstName,
            string.IsNullOrWhiteSpace(lastName) ? null : lastName,
            skillLevel,
            string.IsNullOrWhiteSpace(bio) ? null : bio);

        var updateResult = await sender.Send(updateCommand);
        if (!updateResult.IsSuccess)
        {
            // Return 422 for validation errors, other error codes for other issues
            if (updateResult.Status == ResultStatus.Invalid)
            {
                return Results.UnprocessableEntity(new 
                {
                    errors = updateResult.ValidationErrors.Select(e => e.ErrorMessage).ToList()
                });
            }
            return updateResult.ToMinimalApiResult();
        }

        // Handle photo upload if provided
        var photoFile = form.Files["Photo"];
        if (photoFile != null && photoFile.Length > 0)
        {
            using var stream = photoFile.OpenReadStream();
            var photoCommand = new UploadUserProfilePhotoCommand(
                externalId,
                stream,
                photoFile.ContentType);

            var photoResult = await sender.Send(photoCommand);
            if (!photoResult.IsSuccess)
            {
                return photoResult.ToMinimalApiResult();
            }
        }

        // Return updated profile
        var profileQuery = new GetCurrentUserProfileQuery(externalId);
        var profileResult = await sender.Send(profileQuery);
        return profileResult.ToMinimalApiResult();
    }

    // GET /users/{externalId} - Get user by ExternalId
    private static async Task<HttpResult> GetUserById(
        string externalId,
        ISender sender)
    {
        var query = new GetUserByIdQuery(externalId);
        var result = await sender.Send(query);
        return result.ToMinimalApiResult();
    }

    // POST /users/claim-guest-participations - Claim guest participations
    private static async Task<HttpResult> ClaimGuestParticipations(
        ClaimGuestParticipationsRequest request,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var command = new ClaimGuestParticipationsCommand(
            externalId,
            request.PhoneNumber,
            request.Email);

        var result = await sender.Send(command);

        // Return 422 for validation errors (e.g., missing contact info)
        if (result.Status == ResultStatus.Invalid)
        {
            return Results.UnprocessableEntity(new
            {
                errors = result.ValidationErrors.Select(e => e.ErrorMessage).ToList()
            });
        }

        return result.ToMinimalApiResult();
    }

}

// Request DTOs
internal record SyncUserFromAuthRequest(
    string ExternalId,
    string Name,
    int? SkillLevel,
    string? FirstName = null,  // NEW: First name only
    string? LastName = null,   // NEW: Last name
    string? PhoneNumber = null,
    string? Email = null);
internal record CreateGuestUserRequest(
    string Name,
    int SkillLevel,
    string? PhoneNumber = null,
    string? Email = null);
internal record UpdateUserProfileRequest(string Name, int SkillLevel, string? Bio);
internal record ClaimGuestParticipationsRequest(string? PhoneNumber, string? Email);

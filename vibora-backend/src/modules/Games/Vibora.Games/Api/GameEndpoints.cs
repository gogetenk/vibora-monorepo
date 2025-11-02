using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Vibora.Games.Application.Commands.CancelGame;
using Vibora.Games.Application.Commands.ConvertGuestParticipations;
using Vibora.Games.Application.Commands.CreateGame;
using Vibora.Games.Application.Commands.CreateGameShare;
using Vibora.Games.Application.Commands.JoinGame;
using Vibora.Games.Application.Commands.JoinGameAsGuest;
using Vibora.Games.Application.Commands.LeaveGame;
using Vibora.Games.Application.Queries.GetAvailableGames;
using Vibora.Games.Application.Queries.GetGameDetails;
using Vibora.Games.Application.Queries.GetGuestParticipationsByContact;
using Vibora.Games.Application.Queries.GetMyGames;
using Vibora.Games.Application.Queries.GetShareByToken;
using Vibora.Games.Application.Queries.GetShareMetadata;
using Vibora.Games.Application.Queries.GetUserGamesCount;
using Vibora.Games.Application.Queries.SearchGames;
using Vibora.Shared.Infrastructure.Caching;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Vibora.Games.Api;

internal static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var gamesGroup = endpoints.MapGroup("/games")
            .WithTags("Games");

        gamesGroup.MapGet("/", GetAvailableGames)
            .WithName("GetAvailableGames")
            .CacheOutput(policy => policy
                .SetVaryByQuery("location", "skillLevel", "fromDate", "toDate", "pageNumber", "pageSize")
                .Expire(TimeSpan.FromSeconds(60))
                .Tag(CacheTags.GamesAvailable))
            .Produces<GetAvailableGamesResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        gamesGroup.MapGet("/search", SearchGames)
            .WithName("SearchGames")
            .CacheOutput(policy => policy
                .SetVaryByQuery("when", "where", "level", "latitude", "longitude", "radiusKm")
                .Expire(TimeSpan.FromSeconds(60))
                .Tag(CacheTags.GamesSearch))
            .Produces<SearchGamesQueryResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        gamesGroup.MapGet("/me", GetMyGames)
            .WithName("GetMyGames")
            .RequireAuthorization()
            .CacheOutput(policy => policy
                .SetVaryByHeader("Authorization") // Isolate cache per JWT token
                .Expire(TimeSpan.FromSeconds(30))
                .VaryByValue(context =>
                {
                    // Add user-specific tag for targeted invalidation
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    return new KeyValuePair<string, string>("user", userId ?? "anonymous");
                }))
            .Produces<MyGamesResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        gamesGroup.MapGet("/{id:guid}", GetGameDetails)
            .WithName("GetGameDetails")
            .CacheOutput(policy => policy
                .Expire(TimeSpan.FromSeconds(60))
                .VaryByValue(context =>
                {
                    // Tag with specific game ID for targeted invalidation
                    var id = context.Request.RouteValues["id"]?.ToString();
                    return new KeyValuePair<string, string>("gameId", id ?? "unknown");
                }))
            .Produces<GameDetailsResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        gamesGroup.MapPost("/", CreateGame)
            .WithName("CreateGame")
            .RequireAuthorization()
            .Produces<CreateGameResult>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        gamesGroup.MapPost("/{id:guid}/players", JoinGame)
            .WithName("JoinGame")
            .RequireAuthorization()
            .Produces<JoinGameResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        gamesGroup.MapPost("/{id:guid}/players/guest", JoinGameAsGuest)
            .WithName("JoinGameAsGuest")
            .Produces<JoinGameAsGuestResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        gamesGroup.MapDelete("/{id:guid}/players", LeaveGame)
            .WithName("LeaveGame")
            .RequireAuthorization()
            .Produces<LeaveGameResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        gamesGroup.MapPost("/{id:guid}/cancel", CancelGame)
            .WithName("CancelGame")
            .RequireAuthorization()
            .Produces<CancelGameResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        gamesGroup.MapPost("/{id:guid}/shares", CreateGameShare)
            .WithName("CreateGameShare")
            .RequireAuthorization()
            .Produces<CreateGameShareResult>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Share endpoints (separate group without /games prefix)
        var sharesGroup = endpoints.MapGroup("/shares")
            .WithTags("Shares");

        sharesGroup.MapGet("/{token}", GetShareByToken)
            .WithName("GetShareByToken")
            .CacheOutput(policy => policy
                .Expire(TimeSpan.FromMinutes(10))
                .VaryByValue(context =>
                {
                    var token = context.Request.RouteValues["token"]?.ToString();
                    return new KeyValuePair<string, string>("shareToken", token ?? "unknown");
                }))
            .Produces<GetShareByTokenResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        sharesGroup.MapGet("/{token}/metadata", GetShareMetadata)
            .WithName("GetShareMetadata")
            .CacheOutput(policy => policy
                .Expire(TimeSpan.FromMinutes(10))
                .VaryByValue(context =>
                {
                    var token = context.Request.RouteValues["token"]?.ToString();
                    return new KeyValuePair<string, string>("shareToken", token ?? "unknown");
                }))
            .Produces<GetShareMetadataResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Cross-module endpoints for Microservices mode
        // These are called by other modules via HTTP (IGamesServiceClient)

        gamesGroup.MapGet("/users/{userExternalId}/count", GetUserGamesCount)
            .WithName("GetUserGamesCount")
            .CacheOutput(policy => policy
                .Expire(TimeSpan.FromSeconds(60))
                .VaryByValue(context =>
                {
                    var userId = context.Request.RouteValues["userExternalId"]?.ToString();
                    return new KeyValuePair<string, string>("userExternalId", userId ?? "unknown");
                }))
            .Produces<GameCountResponse>(StatusCodes.Status200OK);

        gamesGroup.MapPost("/guest-participations/by-contact", GetGuestParticipationsByContact)
            .WithName("GetGuestParticipationsByContact")
            .Produces<List<Vibora.Games.Contracts.Services.GuestParticipationDto>>(StatusCodes.Status200OK);

        gamesGroup.MapPost("/guest-participations/convert", ConvertGuestParticipationsEndpoint)
            .WithName("ConvertGuestParticipations")
            .Produces<ConvertGuestParticipationsApiResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    // GET /games/me - Get user's upcoming games
    private static async Task<HttpResult> GetMyGames(
        HttpContext httpContext,
        ISender sender)
    {
        // Extract ExternalId from JWT (ASP.NET Core maps "sub" to NameIdentifier)
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(externalId))
        {
            return Results.Unauthorized();
        }

        var query = new GetMyGamesQuery(externalId);
        var result = await sender.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // GET /games/{id} - Get game details with participants
    private static async Task<HttpResult> GetGameDetails(
        Guid id,
        ISender sender)
    {
        var query = new GetGameDetailsQuery(id);
        var result = await sender.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // GET /games - Get available games with optional filters
    private static async Task<HttpResult> GetAvailableGames(
        ISender sender,
        [FromQuery] string? location = null,
        [FromQuery] string? skillLevel = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAvailableGamesQuery(
            location,
            skillLevel,
            fromDate,
            toDate,
            pageNumber,
            pageSize
        );

        var result = await sender.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // GET /games/search - Search games with intelligent matching
    private static async Task<HttpResult> SearchGames(
        [FromQuery] string when,
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] string? where = null,
        [FromQuery] int? level = null,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] int radiusKm = 10)
    {
        var query = new SearchGamesQuery(when, where, level, latitude, longitude, radiusKm);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // POST /games/{id}/players - Join a game
    private static async Task<HttpResult> JoinGame(
        Guid id,
        JoinGameRequest request,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var command = new JoinGameCommand(
            id,
            externalId,
            request.UserName,
            request.UserSkillLevel
        );

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // POST /games/{id}/players/guest - Join a game as guest (no account required)
    private static async Task<HttpResult> JoinGameAsGuest(
        Guid id,
        JoinGameAsGuestRequest request,
        ISender sender)
    {
        var command = new JoinGameAsGuestCommand(
            id,
            request.Name,
            request.PhoneNumber,
            request.Email
        );

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // DELETE /games/{id}/players - Leave a game
    private static async Task<HttpResult> LeaveGame(
        Guid id,
        HttpContext httpContext,
        ISender sender)
    {
        // Extract ExternalId from JWT sub claim
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var command = new LeaveGameCommand(id, externalId);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // POST /games - Create new game
    private static async Task<HttpResult> CreateGame(
        CreateGameRequest request,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        // Convert numeric skill level to string
        var skillLevelStr = request.SkillLevel?.ToString() ?? "5"; // Default to level 5

        var command = new CreateGameCommand(
            externalId, // User ExternalId from JWT
            request.DateTime,
            request.Location,
            skillLevelStr,
            request.MaxPlayers ?? 4, // Default padel is 4 players
            request.Latitude,
            request.Longitude);

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/games/{result.Value.Id}", result.Value)
            : result.ToMinimalApiResult();
    }

    // POST /games/{id}/cancel - Cancel a game (host only)
    private static async Task<HttpResult> CancelGame(
        Guid id,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var command = new CancelGameCommand(id, externalId);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // POST /games/{id}/shares - Create a shareable link for a game
    private static async Task<HttpResult> CreateGameShare(
        Guid id,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var command = new CreateGameShareCommand(id, externalId);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/shares/{result.Value.ShareToken}", result.Value)
            : result.ToMinimalApiResult();
    }

    // GET /shares/{token} - Get share by token and increment view count
    private static async Task<HttpResult> GetShareByToken(
        string token,
        ISender sender)
    {
        var query = new GetShareByTokenQuery(token);
        var result = await sender.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // GET /shares/{token}/metadata - Get Open Graph metadata for social media previews
    private static async Task<HttpResult> GetShareMetadata(
        string token,
        ISender sender)
    {
        var query = new GetShareMetadataQuery(token);
        var result = await sender.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToMinimalApiResult();
    }

    // Cross-module endpoints (called by other services via HTTP in Microservices mode)

    // GET /games/users/{userExternalId}/count
    private static async Task<HttpResult> GetUserGamesCount(
        string userExternalId,
        ISender sender)
    {
        var query = new GetUserGamesCountQuery(userExternalId);
        var result = await sender.Send(query);

        return result.IsSuccess
            ? Results.Ok(new GameCountResponse(result.Value))
            : Results.Ok(new GameCountResponse(0)); // Graceful degradation
    }

    // POST /games/guest-participations/by-contact
    private static async Task<HttpResult> GetGuestParticipationsByContact(
        GetGuestParticipationsByContactApiRequest request,
        ISender sender)
    {
        var query = new GetGuestParticipationsByContactQuery(request.PhoneNumber, request.Email);
        var result = await sender.Send(query);

        if (!result.IsSuccess)
        {
            return Results.Ok(new List<Vibora.Games.Contracts.Services.GuestParticipationDto>());
        }

        // Map to public contract DTOs
        var dtos = result.Value.GuestParticipations
            .Select(gp => new Vibora.Games.Contracts.Services.GuestParticipationDto(
                gp.GuestParticipantId,
                gp.GameId,
                gp.Name,
                gp.PhoneNumber,
                gp.Email,
                gp.JoinedAt))
            .ToList();

        return Results.Ok(dtos);
    }

    // POST /games/guest-participations/convert
    private static async Task<HttpResult> ConvertGuestParticipationsEndpoint(
        ConvertGuestParticipationsApiRequest request,
        ISender sender)
    {
        var command = new ConvertGuestParticipationsCommand(
            request.GuestParticipantIds,
            request.UserExternalId,
            request.UserName,
            request.UserSkillLevel);

        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Ok(new ConvertGuestParticipationsApiResponse(result.Value.ConvertedCount))
            : Results.Ok(new ConvertGuestParticipationsApiResponse(0)); // Graceful degradation
    }
}

// Request DTOs
internal record CreateGameRequest(
    DateTime DateTime,
    string Location,
    int? SkillLevel, // Accept numeric skill level (1-10) from frontend
    int? MaxPlayers,
    double? Latitude = null,
    double? Longitude = null,
    string? HostExternalId = null // Only for integration tests
);

internal record JoinGameRequest(
    string UserName,
    string UserSkillLevel,
    string? UserExternalId = null // Only for integration tests
);

internal record JoinGameAsGuestRequest(
    string Name,
    string? PhoneNumber,
    string? Email);

internal record GameCountResponse(int Count);

internal record GetGuestParticipationsByContactApiRequest(
    string? PhoneNumber,
    string? Email);

internal record ConvertGuestParticipationsApiRequest(
    List<Guid> GuestParticipantIds,
    string UserExternalId,
    string UserName,
    string UserSkillLevel);

internal record ConvertGuestParticipationsApiResponse(int ConvertedCount);

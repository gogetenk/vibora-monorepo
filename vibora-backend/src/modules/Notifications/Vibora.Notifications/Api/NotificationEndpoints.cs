using System.Security.Claims;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vibora.Notifications.Application.Commands.DeleteNotification;
using Vibora.Notifications.Application.Commands.MarkAsRead;
using Vibora.Notifications.Application.Queries.GetNotificationHistory;

namespace Vibora.Notifications.Api;

/// <summary>
/// Minimal API endpoints for Notifications module
/// All endpoints are internal - only ServiceRegistrar is public
/// </summary>
internal static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var notificationsGroup = endpoints.MapGroup("/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        notificationsGroup.MapGet("", GetNotificationHistory)
            .WithName("GetNotificationHistory")
            .Produces<List<NotificationHistoryDto>>(StatusCodes.Status200OK);

        notificationsGroup.MapPut("/{id}/read", MarkAsRead)
            .WithName("MarkAsRead")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        notificationsGroup.MapDelete("/{id}", DeleteNotification)
            .WithName("DeleteNotification")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetNotificationHistory(
        HttpContext httpContext,
        ISender sender,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var query = new GetNotificationHistoryQuery(externalId, pageNumber, pageSize);
        var result = await sender.Send(query);

        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> MarkAsRead(
        Guid id,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var command = new MarkAsReadCommand(id, externalId);
        var result = await sender.Send(command);

        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> DeleteNotification(
        Guid id,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var command = new DeleteNotificationCommand(id, externalId);
        var result = await sender.Send(command);

        return result.ToMinimalApiResult();
    }
}

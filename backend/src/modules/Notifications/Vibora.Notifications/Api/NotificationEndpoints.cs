using System.Security.Claims;
using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vibora.Notifications.Application.Commands.DeleteNotification;
using Vibora.Notifications.Application.Commands.MarkAsRead;
using Vibora.Notifications.Application.Commands.RegisterDeviceToken;
using Vibora.Notifications.Application.Commands.UpdateNotificationPreferences;
using Vibora.Notifications.Application.Queries.GetNotificationHistory;
using Vibora.Notifications.Application.Queries.GetNotificationPreferences;

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

        notificationsGroup.MapGet("/history", GetNotificationHistory)
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

        // Device token endpoints
        notificationsGroup.MapPost("/device-tokens", RegisterDeviceToken)
            .WithName("RegisterDeviceToken")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // Notification preferences endpoints
        notificationsGroup.MapGet("/preferences", GetNotificationPreferences)
            .WithName("GetNotificationPreferences")
            .Produces<GetNotificationPreferencesResult>(StatusCodes.Status200OK);

        notificationsGroup.MapPut("/preferences", UpdateNotificationPreferences)
            .WithName("UpdateNotificationPreferences")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

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

    // POST /notifications/device-tokens - Register device token for push notifications
    private static async Task<IResult> RegisterDeviceToken(
        RegisterDeviceTokenRequest request,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var command = new RegisterDeviceTokenCommand(externalId, request.DeviceToken);
        var result = await sender.Send(command);
        return result.ToMinimalApiResult();
    }

    // GET /notifications/preferences - Get notification preferences
    private static async Task<IResult> GetNotificationPreferences(
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var query = new GetNotificationPreferencesQuery(externalId);
        var result = await sender.Send(query);
        return result.ToMinimalApiResult();
    }

    // PUT /notifications/preferences - Update notification preferences
    private static async Task<IResult> UpdateNotificationPreferences(
        UpdateNotificationPreferencesRequest request,
        HttpContext httpContext,
        ISender sender)
    {
        var externalId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(externalId))
            return Results.Unauthorized();

        var command = new UpdateNotificationPreferencesCommand(
            externalId,
            Email: null, // Not needed for updates
            request.PushEnabled,
            request.SmsEnabled,
            request.EmailEnabled);
        var result = await sender.Send(command);
        return result.ToMinimalApiResult();
    }
}

// Request DTOs
internal record RegisterDeviceTokenRequest(string DeviceToken);
internal record UpdateNotificationPreferencesRequest(bool? PushEnabled = null, bool? SmsEnabled = null, bool? EmailEnabled = null);

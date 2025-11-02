using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vibora.Games.Infrastructure.Data;

namespace Vibora.Games.Application.Queries.GetGuestParticipationsByContact;

internal sealed class GetGuestParticipationsByContactQueryHandler
    : IRequestHandler<GetGuestParticipationsByContactQuery, Result<GetGuestParticipationsByContactResult>>
{
    private readonly GamesDbContext _dbContext;

    public GetGuestParticipationsByContactQueryHandler(GamesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<GetGuestParticipationsByContactResult>> Handle(
        GetGuestParticipationsByContactQuery request,
        CancellationToken cancellationToken)
    {
        // Validation: At least one contact method must be provided
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) && string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<GetGuestParticipationsByContactResult>.Invalid(
                new ValidationError("Either phone number or email is required"));
        }

        // Build query to find matching guest participations
        var query = _dbContext.GuestParticipants.AsQueryable();

        // Match by phone number or email (case-insensitive)
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !string.IsNullOrWhiteSpace(request.Email))
        {
            // Both provided: match either
            query = query.Where(gp =>
                (gp.PhoneNumber != null && gp.PhoneNumber == request.PhoneNumber) ||
                (gp.Email != null && gp.Email == request.Email.ToLower()));
        }
        else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            // Only phone provided
            query = query.Where(gp => gp.PhoneNumber != null && gp.PhoneNumber == request.PhoneNumber);
        }
        else if (!string.IsNullOrWhiteSpace(request.Email))
        {
            // Only email provided
            query = query.Where(gp => gp.Email != null && gp.Email == request.Email.ToLower());
        }

        var guestParticipations = await query
            .OrderBy(gp => gp.JoinedAt)
            .ToListAsync(cancellationToken);

        var dtos = guestParticipations.Select(gp => new GuestParticipationDto(
            gp.Id,
            gp.GameId,
            gp.Name,
            gp.PhoneNumber,
            gp.Email,
            gp.JoinedAt
        )).ToList();

        return Result.Success(new GetGuestParticipationsByContactResult(dtos));
    }
}

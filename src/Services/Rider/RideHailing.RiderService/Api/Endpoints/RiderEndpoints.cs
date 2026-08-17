using Microsoft.EntityFrameworkCore;
using RideHailing.RiderService.Domain.Entities;
using RideHailing.RiderService.Infrastructure.Persistence;

namespace RideHailing.RiderService.Api.Endpoints;

public static class RiderEndpoints
{
    public static IEndpointRouteBuilder MapRiderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/riders").WithTags("Riders");

        group.MapPost("/", async (
            CreateRiderRequest request,
            RiderDbContext db,
            CancellationToken cancellationToken) =>
        {
            var emailExists = await db.Riders
                .AnyAsync(
                    x => x.Email == request.Email,
                    cancellationToken);

            if (emailExists)
            {
                return Results.Conflict(
                    "A rider with this email already exists.");
            }

            var rider = new Rider(
                request.Name,
                request.Email,
                request.PhoneNumber);

            db.Riders.Add(rider);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/api/riders/{rider.Id}",
                rider);
        })
        .WithName("CreateRider")
        .WithSummary("Create a new rider")
        .WithDescription("Creates a new rider. Returns 201 with the created rider or 409 if the email already exists.")
        .Produces<Rider>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status400BadRequest); ;

        group.MapGet("/{id:guid}", async (
            Guid id,
            RiderDbContext db,
            CancellationToken cancellationToken) =>
        {
            var rider = await db.Riders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            return rider is null
                ? Results.NotFound()
                : Results.Ok(rider);
        })
        .WithName("GetRider")
        .WithSummary("Get a rider by ID")
        .WithDescription("Retrieves a rider by their ID.")
        .Produces<Rider>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private sealed record CreateRiderRequest(
        string Name,
        string Email,
        string PhoneNumber);
}
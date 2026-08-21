using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RideHailing.DriverService.Contracts.Events;
using RideHailing.DriverService.Domain.Entities;
using RideHailing.DriverService.Infrastructure.Persistence;
using System.Text.Json;

namespace RideHailing.DriverService.Api.Endpoints;

public static class DriverEndpoints
{
    public static IEndpointRouteBuilder MapDriverEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/drivers").WithTags("Drivers");

        group.MapPost("/", CreateDriverAsync)
            .WithName("CreateDriver")
            .WithSummary("Create a new driver")
            .WithDescription("Creates a new driver. Returns 201 with the created driver or 409 if there are conflicts.")
            .Produces<DriverResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetDriverAsync)
            .WithName("GetDriverById")
            .WithSummary("Get driver by id")
            .WithDescription("Returns the driver including vehicle if found, otherwise 404.")
            .Produces<DriverResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/", GetDriversAsync)
            .WithName("GetDrivers")
            .WithSummary("List drivers")
            .WithDescription("Returns a list of drivers, ordered by creation date.")
            .Produces<IEnumerable<DriverResponse>>(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}", UpdateDriverAsync)
            .WithName("UpdateDriver")
            .WithSummary("Update a driver's profile")
            .WithDescription("Updates name and phone number for the specified driver. Returns 200 with updated driver or 404 if not found.")
            .Produces<DriverResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost(
            "/{id:guid}/suspend",
            SuspendDriverAsync)
            .WithName("SuspendDriver")
            .WithDescription("Suspends a driver by id.")
            .WithTags("Driver");

        group.MapPost(
            "/{id:guid}/activate",
            ActivateDriverAsync)
            .WithName("ActivateDriver")
            .WithDescription("Activates a suspended driver by id.")
            .WithTags("Driver");

        group.MapPost(
            "/{id:guid}/deactivate",
            DeactivateDriverAsync)
            .WithName("DeactivateDriver")
            .WithDescription("Deactivates an active driver by id.")
            .WithTags("Driver");

        return app;
    }

    private static async Task<IResult> CreateDriverAsync(
        CreateDriverRequest request,
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var emailExists = await db.Drivers
            .AnyAsync(
                x => x.Email == request.Email,
                cancellationToken);

        if (emailExists)
        {
            return Results.Conflict(
                new
                {
                    message =
                        "A driver with this email already exists."
                });
        }

        var phoneExists = await db.Drivers
            .AnyAsync(
                x => x.PhoneNumber == request.PhoneNumber,
                cancellationToken);

        if (phoneExists)
        {
            return Results.Conflict(
                new
                {
                    message =
                        "A driver with this phone already exists."
                });
        }

        var licenseExists = await db.Drivers
            .AnyAsync(
                x => x.LicenseNumber == request.LicenseNumber,
                cancellationToken);

        if (licenseExists)
        {
            return Results.Conflict(
                new
                {
                    message =
                        "A driver with this license already exists."
                });
        }

        var driver = new Driver(
            request.Name,
            request.Email,
            request.PhoneNumber,
            request.LicenseNumber);

        db.Drivers.Add(driver);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/drivers/{driver.Id}",
            ToResponse(driver));
    }

    private static async Task<IResult> GetDriverAsync(
        Guid id,
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        return driver is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(driver));
    }

    private static async Task<IResult> GetDriversAsync(
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var drivers = await db.Drivers
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);

        return Results.Ok(drivers);
    }

    private static async Task<IResult> UpdateDriverAsync(
        Guid id,
        UpdateDriverRequest request,
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (driver is null)
        {
            return Results.NotFound();
        }

        var phoneExists = await db.Drivers
            .AnyAsync(
                x => x.PhoneNumber == request.PhoneNumber
                     && x.Id != id,
                cancellationToken);

        if (phoneExists)
        {
            return Results.Conflict(
                new
                {
                    message =
                        "A driver with this phone already exists."
                });
        }

        driver.UpdateProfile(
            request.Name,
            request.PhoneNumber);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToResponse(driver));
    }

    private static async Task<IResult> SuspendDriverAsync(
        Guid id,
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (driver is null)
        {
            return Results.NotFound();
        }

        try
        {
            driver.Suspend();

            var eventId = Guid.NewGuid();
            var @event = new DriverSuspendedEvent(
                                    eventId,
                                    driver.Id,
                                    DateTime.UtcNow
                                );

            var outboxMessage = new OutboxMessage(
                                    nameof(DriverSuspendedEvent),
                                    JsonSerializer.Serialize(@event)
                                   );

            db.OutboxMessages.Add(outboxMessage);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new
            {
                message = ex.Message
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new
            {
                message =
                    "Driver was modified by another request. Please retry."
            });
        }
    }

    private static async Task<IResult> ActivateDriverAsync(
        Guid id,
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (driver is null)
        {
            return Results.NotFound();
        }

        try
        {
            driver.Activate();

            var eventId = Guid.NewGuid();
            var @event = new DriverActivatedEvent(
                                    eventId,
                                    driver.Id,
                                    DateTime.UtcNow
                                );
            var outboxMessage = new OutboxMessage(
                                    nameof(DriverActivatedEvent),
                                    JsonSerializer.Serialize(@event)
                                   );

            db.OutboxMessages.Add(outboxMessage);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new
            {
                message = ex.Message
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new
            {
                message =
                    "Driver was modified by another request. Please retry."
            });
        }
    }

    private static async Task<IResult> DeactivateDriverAsync(
        Guid id,
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var driver = await db.Drivers
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (driver is null)
        {
            return Results.NotFound();
        }

        driver.Deactivate();

        try
        {
            var eventId = Guid.NewGuid();
            var @event = new DriverDeactivatedEvent(
                                    eventId,
                                    driver.Id,
                                    DateTime.UtcNow
                                );
            var outboxMessage = new OutboxMessage(
                                    nameof(DriverDeactivatedEvent),
                                    JsonSerializer.Serialize(@event)
                                   );

            db.OutboxMessages.Add(outboxMessage);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new
            {
                message =
                    "Driver was modified by another request. Please retry."
            });
        }
    }

    private static DriverResponse ToResponse(
        Driver driver)
    {
        return new DriverResponse(
            driver.Id,
            driver.Name,
            driver.Email,
            driver.PhoneNumber,
            driver.LicenseNumber,
            driver.Rating,
            driver.CreatedAtUtc,
            driver.Vehicle is null
                ? null
                : new VehicleResponse(
                    driver.Vehicle.Id,
                    driver.Vehicle.Make,
                    driver.Vehicle.Model,
                    driver.Vehicle.Color,
                    driver.Vehicle.LicensePlate,
                    driver.Vehicle.ManufacturingYear));
    }

    private sealed record CreateDriverRequest(
        string Name,
        string Email,
        string PhoneNumber,
        string LicenseNumber);

    private sealed record UpdateDriverRequest(
        string Name,
        string PhoneNumber);

    private sealed record DriverResponse(
        Guid Id,
        string Name,
        string Email,
        string PhoneNumber,
        string LicenseNumber,
        decimal Rating,
        DateTime CreatedAtUtc,
        VehicleResponse? Vehicle);

    private sealed record VehicleResponse(
        Guid Id,
        string Make,
        string Model,
        string Color,
        string LicensePlate,
        int ManufacturingYear);
}
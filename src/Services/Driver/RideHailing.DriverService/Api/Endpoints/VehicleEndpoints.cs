using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RideHailing.DriverService.Domain.Entities;
using RideHailing.DriverService.Infrastructure.Persistence;

namespace RideHailing.DriverService.Api.Endpoints;

public static class VehicleEndpoints
{
    public static IEndpointRouteBuilder MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/drivers/{driverId:guid}/vehicle")
            .WithTags("Vehicles");

        group.MapPost("/", CreateVehicleAsync)
            .WithName("CreateVehicle")
            .WithSummary("Create a vehicle for a driver")
            .WithDescription("Creates a vehicle for the specified driver. Returns 201 with the created vehicle or 409 if conflicts occur.")
            .Produces<VehicleResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/", GetVehicleAsync)
            .WithName("GetVehicle")
            .WithSummary("Get a driver's vehicle")
            .WithDescription("Returns the vehicle associated with the specified driver or 404 if none exists.")
            .Produces<VehicleResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/", UpdateVehicleAsync)
            .WithName("UpdateVehicle")
            .WithSummary("Update a driver's vehicle")
            .WithDescription("Updates vehicle details for the specified driver. Returns 200 with the updated vehicle or 404 if not found.")
            .Produces<VehicleResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/", DeleteVehicleAsync)
            .WithName("DeleteVehicle")
            .WithSummary("Delete a driver's vehicle")
            .WithDescription("Removes the vehicle associated with the specified driver. Returns 204 on success or 404 if not found.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateVehicleAsync(Guid driverId, CreateVehicleRequest request, DriverDbContext db, CancellationToken cancellationToken)
    {
        var driver = await db.Drivers.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.Id == driverId, cancellationToken);

        if (driver is null)
        {
            return Results.NotFound(new { message = "Driver not found." });
        }

        if (driver.Vehicle is not null)
        {
            return Results.Conflict(new { message = "Driver already has a vehicle." });
        }

        var licenseExists = await db.Vehicles.AnyAsync(x => x.LicensePlate == request.LicensePlate, cancellationToken);

        if (licenseExists)
        {
            return Results.Conflict(new { message = "Vehicle with this license plate already exists." });
        }

        var vehicle = new Vehicle(
            driverId,
            request.Make,
            request.Model,
            request.Color,
            request.LicensePlate,
            request.ManufacturingYear);

        db.Vehicles.Add(vehicle);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/drivers/{driverId}/vehicle", ToResponse(vehicle));
    }

    private static async Task<IResult> GetVehicleAsync(
        Guid driverId,
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.DriverId == driverId,
                cancellationToken);

        return vehicle is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(vehicle));
    }

    private static async Task<IResult> UpdateVehicleAsync(
        Guid driverId,
        UpdateVehicleRequest request,
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles
            .FirstOrDefaultAsync(
                x => x.DriverId == driverId,
                cancellationToken);

        if (vehicle is null)
        {
            return Results.NotFound();
        }

        vehicle.Update(
            request.Make,
            request.Model,
            request.Color);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToResponse(vehicle));
    }

    private static async Task<IResult> DeleteVehicleAsync(
        Guid driverId,
        DriverDbContext db,
        CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles
            .FirstOrDefaultAsync(
                x => x.DriverId == driverId,
                cancellationToken);

        if (vehicle is null)
        {
            return Results.NotFound();
        }

        db.Vehicles.Remove(vehicle);

        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static VehicleResponse ToResponse(
        Vehicle vehicle)
    {
        return new VehicleResponse(
            vehicle.Id,
            vehicle.DriverId,
            vehicle.Make,
            vehicle.Model,
            vehicle.Color,
            vehicle.LicensePlate,
            vehicle.ManufacturingYear);
    }

    private sealed record CreateVehicleRequest(
        string Make,
        string Model,
        string Color,
        string LicensePlate,
        int ManufacturingYear);

    private sealed record UpdateVehicleRequest(
        string Make,
        string Model,
        string Color);

    private sealed record VehicleResponse(
        Guid Id,
        Guid DriverId,
        string Make,
        string Model,
        string Color,
        string LicensePlate,
        int ManufacturingYear);
}
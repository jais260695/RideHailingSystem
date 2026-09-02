using System.Text.Json;
using RideHailing.LocationService.Application.Events;

namespace RideHailing.LocationService.Infrastructure.Postgres;

public static class DriverEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };


    public static OutboxMessage Create<T>(T message)where T : IDriverEvent
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),

            EventType = typeof(T).Name,

            AggregateType = "Driver",

            AggregateId = message.DriverId.ToString(),

            Payload = JsonSerializer.Serialize(message,Options),

            OccurredAtUtc = message.OccurredAtUtc
        };
    }
}
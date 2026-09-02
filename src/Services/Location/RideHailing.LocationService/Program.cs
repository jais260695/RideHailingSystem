using Confluent.Kafka;
using RideHailing.LocationService.Application;
using RideHailing.LocationService.Hubs;
using RideHailing.LocationService.Infrastructure.Kafka;
using RideHailing.LocationService.Infrastructure.Redis;
using RideHailing.LocationService.Security;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? throw new InvalidOperationException("Redis:ConnectionString is not configured.");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<ILocationStore, RedisLocationStore>();
builder.Services.AddScoped<IDriverIdentity, DriverIdentity>();
builder.Services.AddSignalR();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var configuration = builder.Configuration;
    var bootstrapServers = configuration["Kafka:BootstrapServers"]
                           ?? throw new InvalidOperationException("Kafka:BootstrapServers is not configured.");

    var producerConfig = new ProducerConfig
    {
        BootstrapServers = bootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true
    };

    return new ProducerBuilder<string, string>(producerConfig).Build();
});

builder.Services.AddSingleton<IKafkaPublisher, KafkaPublisher>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { service = "RideHailing.LocationService", status = "Healthy" }));

app.MapHub<DriverHub>("/hubs/driver").RequireAuthorization();

app.Run();
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using RideHailing.LocationService.Application;
using RideHailing.LocationService.Background;
using RideHailing.LocationService.Hubs;
using RideHailing.LocationService.Infrastructure.Kafka;
using RideHailing.LocationService.Infrastructure.Postgres;
using RideHailing.LocationService.Infrastructure.Redis;
using RideHailing.LocationService.Security;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? throw new InvalidOperationException("Redis connection string is missing.");

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is missing.");

var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    ?? throw new InvalidOperationException("Kafka bootstrap servers are missing.");

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

// PostgreSQL
builder.Services.AddPooledDbContextFactory<OutboxDbContext>(options => options.UseNpgsql(postgresConnectionString));

// Application
builder.Services.AddSingleton<ILocationStore, RedisLocationStore>();
builder.Services.AddScoped<IDriverService, DriverService>();

// Outbox
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

// Kafka
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
{
    var config = new ProducerConfig { BootstrapServers = kafkaBootstrapServers, Acks = Acks.All, EnableIdempotence = true };
    return new ProducerBuilder<string, string>(config).Build();
});
builder.Services.AddSingleton<IKafkaPublisher, KafkaPublisher>();

// Background workers
builder.Services.AddHostedService<OutboxPublisherWorker>();

// SignalR
builder.Services.AddSignalR();

// Authentication/Authorization
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IDriverIdentity, DriverIdentity>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { service = "RideHailing.LocationService", status = "Healthy" }));
app.MapHub<DriverHub>("/hubs/driver").RequireAuthorization();

app.Run();
using Microsoft.EntityFrameworkCore;
using RideHailing.DriverService.Infrastructure.Persistence;
using RideHailing.DriverService.Infrastructure.Redis;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services required for Minimal API Swagger generation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DriverDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DriverDatabase"));
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString =  builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is missing.");

    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddSingleton<DriverLocationStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "RideHailing Driver Service");

app.Run();
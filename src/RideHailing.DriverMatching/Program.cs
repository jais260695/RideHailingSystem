using Microsoft.EntityFrameworkCore;
using RideHailing.DriverMatching.Consumers;
using RideHailing.DriverMatching.Handlers;
using RideHailing.DriverMatching.Persistence;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<ConsumerDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ProcessedEventRepository>();
builder.Services.AddScoped<DriverEventHandler>();
builder.Services.AddHostedService<DriverEventConsumer>();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.Run();
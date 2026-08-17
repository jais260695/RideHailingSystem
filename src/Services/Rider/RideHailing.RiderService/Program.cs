using Microsoft.EntityFrameworkCore;
using RideHailing.RiderService.Api.Endpoints;
using RideHailing.RiderService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services required for Minimal API Swagger generation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<RiderDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("RiderDatabase"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapRiderEndpoints();

app.MapGet("/", () => "RideHailing Rider Service");

app.Run();
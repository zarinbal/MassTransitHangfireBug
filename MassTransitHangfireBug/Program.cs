using Hangfire;
using Hangfire.MemoryStorage;
using HealthChecks.SqlServer;
using MassTransit;
using MassTransitHangfireBug.Consumers;
using Microsoft.Extensions.Configuration;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var services = builder.Services;




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();

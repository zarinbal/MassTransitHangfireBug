
using Hangfire;
using MassTransitHangfireBug.MassStartup;
using MassTransitHangfireBug.Objects;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var services = builder.Services;


Enum.TryParse<MassSchedulerType>(builder.Configuration["MassTransit:Scheduler"], out var schedulerType);

switch (schedulerType)  
{
    case MassSchedulerType.SQL:
        break;
    case MassSchedulerType.Quartz:
        builder.ConfigureQuartz();
        break;
    case MassSchedulerType.Hangfire:
        builder.ConfigureHangFire();
        break;
    default:
        throw new InvalidOperationException("Invalid MassTransit scheduler type configured.");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

switch (schedulerType)
{
    case MassSchedulerType.SQL:
        break;
    case MassSchedulerType.Quartz:
        break;
    case MassSchedulerType.Hangfire:
        app.MapHangfireDashboard();
        break;
    default:
        //throw new InvalidOperationException("Invalid MassTransit scheduler type configured.");
        break;
}
app.Run();

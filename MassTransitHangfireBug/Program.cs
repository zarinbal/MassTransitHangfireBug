using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using MassTransitHangfireBug.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var services = builder.Services;

services.AddHangfireServer();

// Add Hangfire with PostgreSQL storage
services.AddHangfire(config =>
{
    config.UseRecommendedSerializerSettings();
    config.UseMemoryStorage();
});

builder.Services
                .AddMassTransit(x =>
                {
                    x.AddPublishMessageScheduler();

                    x.AddHangfireConsumers();
                    x.AddConsumer<ConvertVideoJobConsumer>();
                    x.AddConsumer<TrackVideoConvertedConsumer>();

                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.UsePublishMessageScheduler();
                        //cfg.UseHangfireScheduler();

                        cfg.ConfigureEndpoints(context);
                    });


                    x.SetInMemorySagaRepositoryProvider();
                    x.AddJobSagaStateMachines(options => options.FinalizeCompleted = true);



                });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHangfireDashboard();

app.Run();
